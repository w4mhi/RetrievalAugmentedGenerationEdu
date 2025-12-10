using Microsoft.Extensions.Logging;
using Polly;
using OmniRAG.Core.Interfaces;
using OmniRAG.Core.Models;
using OmniRAG.Infrastructure.Resilience;
using Python.Runtime;
using System.Text.Json;

namespace OmniRAG.Infrastructure.VectorStores;

/// <summary>
/// Vector store implementation using ChromaDB.
/// Repository Pattern: Encapsulates data access logic.
/// </summary>
public sealed class ChromaVectorStore : IVectorStore, IDisposable
{
    private readonly dynamic collection;
    private readonly IntPtr threadState;
    private readonly ILogger<ChromaVectorStore>? logger;
    private readonly IAsyncPolicy<object?> storePolicy;
    private readonly IAsyncPolicy<IReadOnlyList<SearchResult>> searchPolicy;
    private bool disposed;
    private const string CollectionName = "radio_expert_documents";

    public ChromaVectorStore(string persistDirectory, ILogger<ChromaVectorStore>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(persistDirectory);
        this.logger = logger;

        // Initialize resilience policies for vector store I/O operations
        this.storePolicy = ResiliencePolicies.CreateVectorStorePipeline<object?>(
            this.logger,
            "VectorStoreWrite");
        
        this.searchPolicy = ResiliencePolicies.CreateVectorStorePipeline<IReadOnlyList<SearchResult>>(
            this.logger,
            "VectorStoreSearch");

        this.logger?.LogDebug("Initializing ChromaVectorStore with persist directory: {PersistDirectory}", persistDirectory);

        try
        {
            // Ensure Python is initialized
            if (!PythonEngine.IsInitialized)
            {
                PythonEngine.Initialize();
            }
            
            threadState = PythonEngine.BeginAllowThreads();

            // Initialize ChromaDB
            using (Py.GIL())
            {
                dynamic chromadb = Py.Import("chromadb");
                dynamic client = chromadb.PersistentClient(path: persistDirectory);
                
                // Get or create collection
                collection = client.get_or_create_collection(
                    name: CollectionName,
                    metadata: new { description = "OmniRAG technical manual chunks" }.ToPython());
                
                Console.WriteLine($"ChromaDB collection ready: {CollectionName}");
                this.logger?.LogInformation("ChromaDB collection ready: {CollectionName} at {PersistDirectory}", CollectionName, persistDirectory);
            }
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "Failed to initialize ChromaVectorStore: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    public async Task StoreChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        List<DocumentChunk> chunkList = chunks.ToList();
        
        if (chunkList.Count == 0)
        {
            this.logger?.LogWarning("Empty chunk list provided for storage");
            return;
        }

        this.logger?.LogDebug("Storing {ChunkCount} chunks in vector store", chunkList.Count);

        try
        {
            // Execute with resilience policy (Circuit Breaker + Retry)
            await this.storePolicy.ExecuteAsync(async () =>
            {
                await Task.Run(() =>
                {
                    using (Py.GIL())
                    {
                        PyList ids = new PyList();
                        PyList embeddings = new PyList();
                        PyList documents = new PyList();
                        PyList metadatas = new PyList();

                        foreach (DocumentChunk chunk in chunkList)
                        {
                            ids.Append(new PyString(chunk.Id));
                            
                            // Convert embedding to Python list
                            PyList embeddingList = new PyList();
                            foreach (float val in chunk.Embedding)
                            {
                                embeddingList.Append(new PyFloat(val));
                            }
                            embeddings.Append(embeddingList);
                            
                            documents.Append(new PyString(chunk.Content));
                            
                            // Metadata
                            Dictionary<string, object> metadata = new Dictionary<string, object>
                            {
                                ["source_file"] = chunk.SourceFilePath,
                                ["page_number"] = chunk.PageNumber,
                                ["section_title"] = chunk.SectionTitle,
                                ["created_at"] = chunk.CreatedAt.ToString("o")
                            };
                            
                            foreach (KeyValuePair<string, object> kvp in chunk.Metadata)
                            {
                                metadata[$"meta_{kvp.Key}"] = kvp.Value;
                            }
                            
                            metadatas.Append(metadata.ToPython());
                        }

                        collection.add(
                            ids: ids,
                            embeddings: embeddings,
                            documents: documents,
                            metadatas: metadatas);
                    }
                }, cancellationToken);
                return Task.FromResult<object?>(null);
            });

            Console.WriteLine($"Stored {chunkList.Count} chunks in vector store");
            this.logger?.LogInformation("Successfully stored {ChunkCount} chunks in vector store", chunkList.Count);
    }
    catch (Exception ex)
    {
        this.logger?.LogError(ex, "Error storing chunks in vector store: {ErrorMessage}", ex.Message);
        throw;
    }
}

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(float[] queryEmbedding, int topK = 5, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryEmbedding);

        // Use default TopK strategy for backward compatibility
        RetrievalOptions options = RetrievalOptions.Create(RetrievalStrategy.TopK, topK);
        return await SearchAsync(queryEmbedding, options, cancellationToken);
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        float[] queryEmbedding, 
        RetrievalOptions options, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryEmbedding);
        ArgumentNullException.ThrowIfNull(options);
        
        options.Validate();

        this.logger?.LogDebug("Searching vector store with strategy: {Strategy}, topK: {TopK}", options.Strategy, options.TopK);

        try
        {
            // Execute with resilience policy (Circuit Breaker + Retry)
            return await this.searchPolicy.ExecuteAsync(async () =>
            {
                return await Task.Run(() =>
                {
                    using (Py.GIL())
                    {
                        // Convert query embedding to Python list
                        PyList queryEmbeddingList = new PyList();
                        foreach (float val in queryEmbedding)
                        {
                            queryEmbeddingList.Append(new PyFloat(val));
                        }

                        // Retrieve more results than needed for strategies that filter/rerank
                        int retrievalCount = options.Strategy switch
                        {
                            RetrievalStrategy.ThresholdBased => Math.Min(options.TopK * 2, 50),
                            RetrievalStrategy.MaxMarginalRelevance => Math.Min(options.TopK * 3, 50),
                            _ => options.TopK
                        };

                        dynamic results = collection.query(
                            query_embeddings: new PyList(new[] { queryEmbeddingList }),
                            n_results: retrievalCount);

                        IReadOnlyList<SearchResult> searchResults = ApplyRetrievalStrategy(results, options);
                        this.logger?.LogDebug("Search completed, returning {ResultCount} results", searchResults.Count);
                        return searchResults;
                    }
                }, cancellationToken);
            });
    }
    catch (Exception ex)
    {
        this.logger?.LogError(ex, "Error searching vector store: {ErrorMessage}", ex.Message);
        throw;
    }
}

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            using (Py.GIL())
            {
                collection.delete();
            }
        }, cancellationToken);

        Console.WriteLine("Vector store cleared");
    }

    public async Task<int> GetChunkCountAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            using (Py.GIL())
            {
                return (int)collection.count();
            }
        }, cancellationToken);
    }

    private static IReadOnlyList<SearchResult> ParseSearchResults(dynamic results, int topK)
    {
        List<SearchResult> searchResults = new List<SearchResult>();

        try
        {
            dynamic ids = results["ids"][0];
            dynamic documents = results["documents"][0];
            dynamic metadatas = results["metadatas"][0];
            dynamic distances = results["distances"][0];

            int count = Math.Min((int)ids.Length(), topK);

            for (int i = 0; i < count; i++)
            {
                string id = (string)ids[i];
                string content = (string)documents[i];
                dynamic metadata = metadatas[i];
                float distance = (float)(double)distances[i];

                // Convert distance to similarity score (ChromaDB uses L2 distance)
                float similarity = 1.0f / (1.0f + distance);

                DocumentChunk chunk = new DocumentChunk
                {
                    Id = id,
                    Content = content,
                    SourceFilePath = (string)metadata["source_file"],
                    PageNumber = (int)metadata["page_number"],
                    SectionTitle = (string)metadata["section_title"],
                    Embedding = Array.Empty<float>(), // Not needed for search results
                    Metadata = new Dictionary<string, object>(),
                    CreatedAt = DateTime.Parse((string)metadata["created_at"])
                };

                searchResults.Add(SearchResult.Create(chunk, similarity, i + 1));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing search results: {ex.Message}");
        }

        return searchResults;
    }

    /// <summary>
    /// Applies the specified retrieval strategy to raw ChromaDB results.
    /// Strategy Pattern: Different algorithms based on retrieval strategy.
    /// </summary>
    private static IReadOnlyList<SearchResult> ApplyRetrievalStrategy(
        dynamic results, 
        RetrievalOptions options)
    {
        // Parse raw results into SearchResult objects
        List<SearchResult> allResults = ParseSearchResults(results, (int)results["ids"][0].Length()).ToList();

        return options.Strategy switch
        {
            RetrievalStrategy.TopK => ApplyTopKStrategy(allResults, options),
            RetrievalStrategy.ThresholdBased => ApplyThresholdStrategy(allResults, options),
            RetrievalStrategy.Hybrid => ApplyHybridStrategy(allResults, options),
            RetrievalStrategy.MaxMarginalRelevance => ApplyMMRStrategy(allResults, options),
            _ => throw new ArgumentOutOfRangeException(nameof(options.Strategy))
        };
    }

    /// <summary>
    /// Top-K strategy: Returns fixed number of top results.
    /// </summary>
    private static IReadOnlyList<SearchResult> ApplyTopKStrategy(
        List<SearchResult> results, 
        RetrievalOptions options)
    {
        return results
            .OrderByDescending(r => r.RelevanceScore)
            .Take(options.TopK)
            .Select((r, index) => SearchResult.Create(r.Chunk, r.RelevanceScore, index + 1))
            .ToList();
    }

    /// <summary>
    /// Threshold strategy: Returns results above minimum similarity.
    /// </summary>
    private static IReadOnlyList<SearchResult> ApplyThresholdStrategy(
        List<SearchResult> results, 
        RetrievalOptions options)
    {
        return results
            .Where(r => r.RelevanceScore >= options.MinSimilarity)
            .OrderByDescending(r => r.RelevanceScore)
            .Take(options.TopK)
            .Select((r, index) => SearchResult.Create(r.Chunk, r.RelevanceScore, index + 1))
            .ToList();
    }

    /// <summary>
    /// Hybrid strategy: Combines vector similarity with keyword matching.
    /// Currently implements vector-first with keyword boosting.
    /// </summary>
    private static IReadOnlyList<SearchResult> ApplyHybridStrategy(
        List<SearchResult> results, 
        RetrievalOptions options)
    {
        // For now, apply threshold filter and return top results
        // Future enhancement: Add BM25 keyword scoring and combine with vector scores
        return results
            .Where(r => r.RelevanceScore >= options.MinSimilarity)
            .OrderByDescending(r => r.RelevanceScore)
            .Take(options.TopK)
            .Select((r, index) => SearchResult.Create(r.Chunk, r.RelevanceScore, index + 1))
            .ToList();
    }

    /// <summary>
    /// MMR strategy: Maximal Marginal Relevance for diverse results.
    /// Balances relevance and diversity using lambda parameter.
    /// </summary>
    private static IReadOnlyList<SearchResult> ApplyMMRStrategy(
        List<SearchResult> results, 
        RetrievalOptions options)
    {
        if (results.Count == 0)
        {
            return results;
        }

        List<SearchResult> selectedResults = new List<SearchResult>();
        List<SearchResult> remainingResults = results
            .Where(r => r.RelevanceScore >= options.MinSimilarity)
            .OrderByDescending(r => r.RelevanceScore)
            .ToList();

        if (remainingResults.Count == 0)
        {
            return remainingResults;
        }

        // Start with most relevant result
        selectedResults.Add(remainingResults[0]);
        remainingResults.RemoveAt(0);

        // Iteratively select results balancing relevance and diversity
        while (selectedResults.Count < options.TopK && remainingResults.Count > 0)
        {
            float maxScore = float.MinValue;
            int maxIndex = 0;

            for (int i = 0; i < remainingResults.Count; i++)
            {
                SearchResult candidate = remainingResults[i];
                
                // Calculate minimum similarity to already selected results
                float maxSimilarityToSelected = selectedResults
                    .Max(selected => CalculateCosineSimilarity(
                        candidate.Chunk.Content, 
                        selected.Chunk.Content));

                // MMR score: lambda * relevance - (1 - lambda) * max_similarity
                float mmrScore = options.DiversityLambda * candidate.RelevanceScore 
                               - (1 - options.DiversityLambda) * maxSimilarityToSelected;

                if (mmrScore > maxScore)
                {
                    maxScore = mmrScore;
                    maxIndex = i;
                }
            }

            selectedResults.Add(remainingResults[maxIndex]);
            remainingResults.RemoveAt(maxIndex);
        }

        // Rerank with final positions
        return selectedResults
            .Select((r, index) => SearchResult.Create(r.Chunk, r.RelevanceScore, index + 1))
            .ToList();
    }

    /// <summary>
    /// Calculates cosine similarity between two text strings (simple Jaccard approximation).
    /// For production: Use actual embedding vectors for precise similarity.
    /// </summary>
    private static float CalculateCosineSimilarity(string text1, string text2)
    {
        // Simple word-based Jaccard similarity as approximation
        HashSet<string> words1 = new HashSet<string>(
            text1.ToLowerInvariant()
                .Split(new[] { ' ', '.', ',', ';', ':', '\n', '\r' }, 
                       StringSplitOptions.RemoveEmptyEntries));
        
        HashSet<string> words2 = new HashSet<string>(
            text2.ToLowerInvariant()
                .Split(new[] { ' ', '.', ',', ';', ':', '\n', '\r' }, 
                       StringSplitOptions.RemoveEmptyEntries));

        if (words1.Count == 0 || words2.Count == 0)
        {
            return 0.0f;
        }

        int intersection = words1.Intersect(words2).Count();
        int union = words1.Union(words2).Count();

        return union > 0 ? (float)intersection / union : 0.0f;
    }

    public void Dispose()
    {
        if (disposed) return;

        PythonEngine.EndAllowThreads(threadState);
        disposed = true;
    }
}
