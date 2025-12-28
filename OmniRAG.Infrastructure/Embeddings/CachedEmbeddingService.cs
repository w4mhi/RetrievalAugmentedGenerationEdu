using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using OmniRAG.Core.Interfaces;

namespace OmniRAG.Infrastructure.Embeddings;

/// <summary>
/// Decorator for IEmbeddingService that adds caching capabilities.
/// Provides 10-100x speedup for repeated queries by caching embeddings.
/// Decorator Pattern: Wraps any IEmbeddingService implementation with caching.
/// Single Responsibility: Handles caching logic only.
/// </summary>
public class CachedEmbeddingService : IEmbeddingService, IDisposable
{
    private readonly IEmbeddingService innerService;
    private readonly IMemoryCache cache;
    private readonly ILogger<CachedEmbeddingService>? logger;
    private readonly TimeSpan cacheExpiration;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedEmbeddingService"/> class.
    /// </summary>
    /// <param name="innerService">The underlying embedding service to cache.</param>
    /// <param name="cache">The memory cache instance.</param>
    /// <param name="cacheExpiration">How long to cache embeddings (default: 24 hours).</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public CachedEmbeddingService(
        IEmbeddingService innerService,
        IMemoryCache cache,
        TimeSpan? cacheExpiration = null,
        ILogger<CachedEmbeddingService>? logger = null)
    {
        this.innerService = innerService ?? throw new ArgumentNullException(nameof(innerService));
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.logger = logger;
        this.cacheExpiration = cacheExpiration ?? TimeSpan.FromHours(24);
    }

    /// <summary>
    /// Generates an embedding for the given text with caching.
    /// </summary>
    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text cannot be null or empty.", nameof(text));
        }

        string cacheKey = this.ComputeCacheKey(text);

        // Try to get from cache
        if (this.cache.TryGetValue<float[]>(cacheKey, out float[]? cachedEmbedding) && cachedEmbedding != null)
        {
            this.logger?.LogDebug(
                "Cache hit for text: {TextPrefix}... (length: {Length})",
                this.TruncateForLogging(text),
                text.Length);
            
            return cachedEmbedding;
        }

        // Cache miss - generate embedding
        this.logger?.LogDebug(
            "Cache miss for text: {TextPrefix}... (length: {Length})",
            this.TruncateForLogging(text),
            text.Length);

        float[] embedding = await this.innerService.GenerateEmbeddingAsync(text, cancellationToken);

        // Store in cache with size-based eviction
        MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = this.cacheExpiration,
            Size = embedding.Length * sizeof(float), // Track memory usage
            Priority = CacheItemPriority.Normal
        };

        this.cache.Set(cacheKey, embedding, cacheEntryOptions);

        this.logger?.LogInformation(
            "Cached embedding for text: {TextPrefix}... (dimension: {Dimension}, cache duration: {Duration})",
            this.TruncateForLogging(text),
            embedding.Length,
            this.cacheExpiration);

        return embedding;
    }

    /// <summary>
    /// Generates embeddings for multiple texts in batch with caching.
    /// </summary>
    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (texts == null)
        {
            throw new ArgumentNullException(nameof(texts));
        }

        List<string> textList = texts.ToList();
        if (textList.Count == 0)
        {
            return Array.Empty<float[]>();
        }

        List<float[]> results = new List<float[]>(textList.Count);
        List<string> uncachedTexts = new List<string>();
        Dictionary<int, string> uncachedIndexMap = new Dictionary<int, string>();

        // Check cache for each text
        for (int i = 0; i < textList.Count; i++)
        {
            string text = textList[i];
            string cacheKey = this.ComputeCacheKey(text);

            if (this.cache.TryGetValue<float[]>(cacheKey, out float[]? cachedEmbedding) && cachedEmbedding != null)
            {
                results.Add(cachedEmbedding);
                this.logger?.LogDebug("Batch cache hit for text at index {Index}", i);
            }
            else
            {
                // Placeholder - will be filled after batch generation
                results.Add(Array.Empty<float>());
                uncachedTexts.Add(text);
                uncachedIndexMap[i] = text;
            }
        }

        // Generate embeddings for uncached texts
        if (uncachedTexts.Count > 0)
        {
            this.logger?.LogInformation(
                "Batch processing: {CachedCount} cached, {UncachedCount} uncached",
                textList.Count - uncachedTexts.Count,
                uncachedTexts.Count);

            IReadOnlyList<float[]> newEmbeddings = await this.innerService.GenerateEmbeddingsAsync(
                uncachedTexts,
                cancellationToken);

            // Cache new embeddings and update results
            int embeddingIndex = 0;
            foreach (KeyValuePair<int, string> kvp in uncachedIndexMap)
            {
                int originalIndex = kvp.Key;
                string text = kvp.Value;
                float[] embedding = newEmbeddings[embeddingIndex++];

                // Cache the new embedding
                string cacheKey = this.ComputeCacheKey(text);
                MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    SlidingExpiration = this.cacheExpiration,
                    Size = embedding.Length * sizeof(float),
                    Priority = CacheItemPriority.Normal
                };
                this.cache.Set(cacheKey, embedding, cacheEntryOptions);

                // Update results
                results[originalIndex] = embedding;
            }
        }
        else
        {
            this.logger?.LogInformation("Batch processing: All {Count} embeddings served from cache", textList.Count);
        }

        return results;
    }

    /// <summary>
    /// Computes a cache key from text using SHA256 hash.
    /// </summary>
    private string ComputeCacheKey(string text)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        string hash = Convert.ToBase64String(hashBytes);
        return $"embedding:{hash}";
    }

    /// <summary>
    /// Truncates text for logging purposes.
    /// </summary>
    private string TruncateForLogging(string text, int maxLength = 50)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }

        return text.Substring(0, maxLength) + "...";
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                // Dispose inner service if it's disposable
                if (this.innerService is IDisposable disposableInner)
                {
                    disposableInner.Dispose();
                }
            }

            this.disposed = true;
        }
    }
}
