using OmniRAG.Core.Models;

namespace OmniRAG.Core.Interfaces;

/// <summary>
/// Repository pattern for vector storage operations.
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Stores document chunks with their embeddings.
    /// </summary>
    Task StoreChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs similarity search for the given query embedding.
    /// Legacy method for backward compatibility.
    /// </summary>
    Task<IReadOnlyList<SearchResult>> SearchAsync(float[] queryEmbedding, int topK = 5, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs similarity search with configurable retrieval options.
    /// Strategy Pattern: Supports multiple retrieval strategies.
    /// </summary>
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        float[] queryEmbedding, 
        RetrievalOptions options, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clears all stored data.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the total count of stored chunks.
    /// </summary>
    Task<int> GetChunkCountAsync(CancellationToken cancellationToken = default);
}
