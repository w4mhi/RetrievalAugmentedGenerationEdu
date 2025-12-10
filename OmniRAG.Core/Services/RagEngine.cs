using Microsoft.Extensions.Logging;
using OmniRAG.Core.Interfaces;
using OmniRAG.Core.Models;
using System.Diagnostics;

namespace OmniRAG.Core.Services;

/// <summary>
/// RAG Engine implementation following SOLID principles.
/// Single Responsibility: Orchestrates RAG workflow.
/// Dependency Inversion: Depends on abstractions (interfaces).
/// </summary>
public sealed class RagEngine : IRagEngine
{
    private readonly IDocumentLoader documentLoader;
    private readonly IEmbeddingService embeddingService;
    private readonly IVectorStore vectorStore;
    private readonly ILanguageModel? languageModel; // Optional: graceful fallback if not configured
    private readonly RetrievalOptions retrievalOptions;
    private readonly ILogger<RagEngine>? logger;
    private DateTime? lastIndexed;

    public RagEngine(
        IDocumentLoader documentLoader,
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        ILanguageModel? languageModel = null,
        RetrievalOptions? retrievalOptions = null,
        ILogger<RagEngine>? logger = null)
    {
        this.documentLoader = documentLoader ?? throw new ArgumentNullException(nameof(documentLoader));
        this.embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        this.vectorStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
        this.languageModel = languageModel; // Optional: allows running without LLM for testing
        this.retrievalOptions = retrievalOptions ?? RetrievalOptions.Create(RetrievalStrategy.TopK);
        this.logger = logger;
        
        this.retrievalOptions.Validate();
        
        this.logger?.LogInformation(
            "RagEngine initialized with {RetrievalStrategy} retrieval strategy, TopK={TopK}, LLM={LlmEnabled}",
            this.retrievalOptions.Strategy,
            this.retrievalOptions.TopK,
            this.languageModel != null ? "Enabled" : "Disabled");
    }

    public async Task<RagResponse> QueryAsync(string query, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        
        this.logger?.LogDebug("Processing query: {Query}", query);
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            // Generate query embedding
            this.logger?.LogDebug("Generating embedding for query");
            float[] queryEmbedding = await this.embeddingService.GenerateEmbeddingAsync(query, cancellationToken);
            this.logger?.LogDebug("Generated embedding with {Dimensions} dimensions", queryEmbedding.Length);

            // Retrieve relevant chunks using configured retrieval strategy
            this.logger?.LogDebug(
                "Searching vector store with {Strategy} strategy, TopK={TopK}",
                this.retrievalOptions.Strategy,
                this.retrievalOptions.TopK);
            
            IReadOnlyList<SearchResult> searchResults = await this.vectorStore.SearchAsync(
                queryEmbedding, 
                this.retrievalOptions, 
                cancellationToken);

            this.logger?.LogInformation(
                "Retrieved {ResultCount} results for query in {ElapsedMs}ms",
                searchResults.Count,
                stopwatch.ElapsedMilliseconds);

            // Generate answer using Phi-4 if available, otherwise fallback to simple retrieval
            string answer;
            if (this.languageModel != null)
            {
                this.logger?.LogDebug("Generating answer using LLM");
                answer = await this.GenerateAnswerWithLlmAsync(query, searchResults, cancellationToken);
            }
            else
            {
                this.logger?.LogWarning("LLM not configured, using fallback retrieval mode");
                answer = GenerateAnswerFallback(query, searchResults);
            }

            stopwatch.Stop();
            
            this.logger?.LogInformation(
                "Query processed successfully in {TotalSeconds}s",
                stopwatch.Elapsed.TotalSeconds);

            return RagResponse.Create(answer, searchResults, stopwatch.Elapsed, query);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            this.logger?.LogError(
                ex,
                "Failed to process query after {ElapsedMs}ms: {ErrorMessage}",
                stopwatch.ElapsedMilliseconds,
                ex.Message);
            throw;
        }
    }

    public async Task IndexDocumentsAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        if (!Directory.Exists(directoryPath))
        {
            this.logger?.LogError("Directory not found: {DirectoryPath}", directoryPath);
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        this.logger?.LogInformation("Starting document indexing from: {DirectoryPath}", directoryPath);
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            // Load documents
            this.logger?.LogDebug("Loading documents from directory");
            IReadOnlyList<DocumentChunk> chunks = await this.documentLoader.LoadDocumentsAsync(directoryPath, cancellationToken);

            if (chunks.Count == 0)
            {
                this.logger?.LogWarning("No documents found in directory: {DirectoryPath}", directoryPath);
                throw new InvalidOperationException("No documents found to index.");
            }

            this.logger?.LogInformation("Loaded {ChunkCount} chunks from documents", chunks.Count);

            // Store chunks in vector store
            this.logger?.LogDebug("Storing chunks in vector store");
            await this.vectorStore.StoreChunksAsync(chunks, cancellationToken);

            this.lastIndexed = DateTime.UtcNow;
            stopwatch.Stop();

            this.logger?.LogInformation(
                "Successfully indexed {ChunkCount} chunks in {ElapsedSeconds}s",
                chunks.Count,
                stopwatch.Elapsed.TotalSeconds);
        }
        catch (Exception ex) when (ex is not InvalidOperationException && ex is not DirectoryNotFoundException)
        {
            stopwatch.Stop();
            this.logger?.LogError(
                ex,
                "Failed to index documents after {ElapsedMs}ms: {ErrorMessage}",
                stopwatch.ElapsedMilliseconds,
                ex.Message);
            throw;
        }
    }

    public async Task<(int TotalChunks, DateTime? LastIndexed)> GetIndexStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            this.logger?.LogDebug("Retrieving index statistics");
            int count = await this.vectorStore.GetChunkCountAsync(cancellationToken);
            this.logger?.LogDebug("Index contains {ChunkCount} chunks, last indexed: {LastIndexed}", count, this.lastIndexed);
            return (count, this.lastIndexed);
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "Failed to retrieve index statistics: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Generates an answer using the LLM with retrieved context.
    /// Uncle Bob's Clean Code: Method does one thing well.
    /// </summary>
    private async Task<string> GenerateAnswerWithLlmAsync(
        string query, 
        IReadOnlyList<SearchResult> searchResults, 
        CancellationToken cancellationToken)
    {
        if (searchResults.Count == 0)
        {
            this.logger?.LogWarning("No search results available for LLM generation");
            return "I couldn't find any relevant information in the documentation to answer your question.";
        }

        try
        {
            // Build context from retrieved chunks
            System.Text.StringBuilder contextBuilder = new System.Text.StringBuilder();
            contextBuilder.AppendLine("Use the following information from technical manuals to answer the user's question:");
            contextBuilder.AppendLine();

            for (int i = 0; i < searchResults.Count; i++)
            {
                SearchResult result = searchResults[i];
                contextBuilder.AppendLine($"--- Source {i + 1} ---");
                contextBuilder.AppendLine($"Document: {Path.GetFileName(result.Chunk.SourceFilePath)}");
                contextBuilder.AppendLine($"Page: {result.Chunk.PageNumber}");
                contextBuilder.AppendLine($"Section: {result.Chunk.SectionTitle}");
                contextBuilder.AppendLine($"Relevance Score: {result.RelevanceScore:P1}");
                contextBuilder.AppendLine();
                contextBuilder.AppendLine(result.Chunk.Content);
                contextBuilder.AppendLine();
            }

            contextBuilder.AppendLine("---");
            contextBuilder.AppendLine();
            contextBuilder.AppendLine($"User Question: {query}");
            contextBuilder.AppendLine();
            contextBuilder.AppendLine("Please provide a clear, accurate answer based on the information above. " +
                                     "Cite specific sources (document name and page number) in your answer. " +
                                     "If the provided information is insufficient, state that clearly.");

            // Generate response using LLM
            string prompt = contextBuilder.ToString();
            this.logger?.LogDebug("Sending prompt to LLM with {ContextLength} characters", prompt.Length);
            
            string answer = await this.languageModel!.GenerateAsync(prompt, cancellationToken);
            
            this.logger?.LogDebug("LLM generated answer with {AnswerLength} characters", answer.Length);
            return answer;
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "LLM generation failed: {ErrorMessage}", ex.Message);
            this.logger?.LogWarning("Falling back to retrieval-only mode due to LLM error");
            return GenerateAnswerFallback(query, searchResults);
        }
    }

    /// <summary>
    /// Fallback method when LLM is not available.
    /// Returns simple concatenation of retrieved chunks.
    /// </summary>
    private static string GenerateAnswerFallback(string query, IReadOnlyList<SearchResult> searchResults)
    {
        if (searchResults.Count == 0)
        {
            return "I couldn't find any relevant information in the documentation.";
        }

        // Simple retrieval without LLM generation
        string context = string.Join("\n\n", searchResults.Select(r => 
            $"[Source: {Path.GetFileName(r.Chunk.SourceFilePath)}, Page {r.Chunk.PageNumber}, Section: {r.Chunk.SectionTitle}]\n{r.Chunk.Content}"));

        return $"Based on the documentation:\n\n{context}\n\n(Note: LLM not configured. Showing retrieved chunks only. Configure Phi-4 in appsettings.json for enhanced answers.)";
    }
}
