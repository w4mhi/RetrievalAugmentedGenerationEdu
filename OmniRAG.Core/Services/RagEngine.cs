using Microsoft.Extensions.Logging;
using OmniRAG.Core.Constants;
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
            LogMessages.RagEngineInitialized,
            this.retrievalOptions.Strategy,
            this.retrievalOptions.TopK,
            this.languageModel != null ? "Enabled" : "Disabled");
    }

    public async Task<RagResponse> QueryAsync(string query, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        
        this.logger?.LogDebug(LogMessages.ProcessingQuery, query);
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            // Generate query embedding
            this.logger?.LogDebug(LogMessages.GeneratingEmbedding);
            float[] queryEmbedding = await this.embeddingService.GenerateEmbeddingAsync(query, cancellationToken);
            this.logger?.LogDebug(LogMessages.EmbeddingGenerated, queryEmbedding.Length);

            // Retrieve relevant chunks using configured retrieval strategy
            this.logger?.LogDebug(
                LogMessages.SearchingVectorStore,
                this.retrievalOptions.Strategy,
                this.retrievalOptions.TopK);
            
            IReadOnlyList<SearchResult> searchResults = await this.vectorStore.SearchAsync(
                queryEmbedding, 
                this.retrievalOptions, 
                cancellationToken);

            this.logger?.LogInformation(
                LogMessages.RetrievedResults,
                searchResults.Count,
                stopwatch.ElapsedMilliseconds);

            // Generate answer using Phi-4 if available, otherwise fallback to simple retrieval
            string answer;
            if (this.languageModel != null)
            {
                this.logger?.LogDebug(LogMessages.GeneratingAnswerWithLlm);
                answer = await this.GenerateAnswerWithLlmAsync(query, searchResults, cancellationToken);
            }
            else
            {
                this.logger?.LogWarning(LogMessages.LlmNotConfigured);
                answer = GenerateAnswerFallback(query, searchResults);
            }

            stopwatch.Stop();
            
            this.logger?.LogInformation(
                LogMessages.QueryProcessedSuccessfully,
                stopwatch.Elapsed.TotalSeconds);

            return RagResponse.Create(answer, searchResults, stopwatch.Elapsed, query);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            this.logger?.LogError(
                ex,
                LogMessages.QueryProcessingFailed,
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
            this.logger?.LogError(LogMessages.DirectoryNotFound, directoryPath);
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        this.logger?.LogInformation(LogMessages.StartingDocumentIndexing, directoryPath);
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            // Load documents
            this.logger?.LogDebug(LogMessages.LoadingDocuments);
            IReadOnlyList<DocumentChunk> chunks = await this.documentLoader.LoadDocumentsAsync(directoryPath, cancellationToken);

            if (chunks.Count == 0)
            {
                this.logger?.LogWarning(LogMessages.NoDocumentsFound, directoryPath);
                throw new InvalidOperationException("No documents found to index.");
            }

            this.logger?.LogInformation(LogMessages.LoadedChunks, chunks.Count);

            // Store chunks in vector store
            this.logger?.LogDebug(LogMessages.StoringChunks);
            await this.vectorStore.StoreChunksAsync(chunks, cancellationToken);

            this.lastIndexed = DateTime.UtcNow;
            stopwatch.Stop();

            this.logger?.LogInformation(
                LogMessages.IndexedSuccessfully,
                chunks.Count,
                stopwatch.Elapsed.TotalSeconds);
        }
        catch (Exception ex) when (ex is not InvalidOperationException && ex is not DirectoryNotFoundException)
        {
            stopwatch.Stop();
            this.logger?.LogError(
                ex,
                LogMessages.IndexingFailed,
                stopwatch.ElapsedMilliseconds,
                ex.Message);
            throw;
        }
    }

    public async Task<(int TotalChunks, DateTime? LastIndexed)> GetIndexStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            this.logger?.LogDebug(LogMessages.RetrievingIndexStats);
            int count = await this.vectorStore.GetChunkCountAsync(cancellationToken);
            this.logger?.LogDebug(LogMessages.IndexStatsRetrieved, count, this.lastIndexed);
            return (count, this.lastIndexed);
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, LogMessages.IndexStatsRetrievalFailed, ex.Message);
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
            this.logger?.LogWarning(LogMessages.NoSearchResults);
            return FallbackMessages.NoRelevantInformation;
        }

        try
        {
            string prompt = BuildLlmPrompt(query, searchResults);
            this.logger?.LogDebug(LogMessages.SendingPromptToLlm, prompt.Length);
            
            string answer = await this.languageModel!.GenerateAsync(prompt, cancellationToken);
            
            this.logger?.LogDebug(LogMessages.LlmGeneratedAnswer, answer.Length);
            return answer;
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, LogMessages.LlmGenerationFailed, ex.Message);
            this.logger?.LogWarning(LogMessages.FallingBackToRetrieval);
            return GenerateAnswerFallback(query, searchResults);
        }
    }

    /// <summary>
    /// Builds LLM prompt from query and search results.
    /// Reduces method complexity (Rule 16).
    /// </summary>
    private static string BuildLlmPrompt(string query, IReadOnlyList<SearchResult> searchResults)
    {
        System.Text.StringBuilder contextBuilder = new System.Text.StringBuilder();
        contextBuilder.AppendLine("Use the following information from technical manuals to answer the user's question:");
        contextBuilder.AppendLine();

        for (int i = 0; i < searchResults.Count; i++)
        {
            AppendSearchResultContext(contextBuilder, searchResults[i], i + 1);
        }

        contextBuilder.AppendLine("---");
        contextBuilder.AppendLine();
        contextBuilder.AppendLine($"User Question: {query}");
        contextBuilder.AppendLine();
        contextBuilder.AppendLine(SystemPrompts.AnswerInstructions);

        return contextBuilder.ToString();
    }

    /// <summary>
    /// Appends a single search result to the context builder.
    /// Reduces method complexity (Rule 16).
    /// </summary>
    private static void AppendSearchResultContext(
        System.Text.StringBuilder contextBuilder, 
        SearchResult result, 
        int sourceNumber)
    {
        contextBuilder.AppendLine($"--- Source {sourceNumber} ---");
        contextBuilder.AppendLine($"Document: {Path.GetFileName(result.Chunk.SourceFilePath)}");
        contextBuilder.AppendLine($"Page: {result.Chunk.PageNumber}");
        contextBuilder.AppendLine($"Section: {result.Chunk.SectionTitle}");
        contextBuilder.AppendLine($"Relevance Score: {result.RelevanceScore:P1}");
        contextBuilder.AppendLine();
        contextBuilder.AppendLine(result.Chunk.Content);
        contextBuilder.AppendLine();
    }

    /// <summary>
    /// Fallback method when LLM is not available.
    /// Returns simple concatenation of retrieved chunks.
    /// </summary>
    private static string GenerateAnswerFallback(string query, IReadOnlyList<SearchResult> searchResults)
    {
        if (searchResults.Count == 0)
        {
            return FallbackMessages.NoRelevantInformationSimple;
        }

        // Simple retrieval without LLM generation
        string context = string.Join("\n\n", searchResults.Select(r => 
            $"[Source: {Path.GetFileName(r.Chunk.SourceFilePath)}, Page {r.Chunk.PageNumber}, Section: {r.Chunk.SectionTitle}]\n{r.Chunk.Content}"));

        return $"Based on the documentation:\n\n{context}\n\n{FallbackMessages.ConfigurePhi4Note}";
    }
}
