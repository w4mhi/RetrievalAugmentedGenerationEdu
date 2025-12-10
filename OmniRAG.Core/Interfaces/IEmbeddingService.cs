namespace OmniRAG.Core.Interfaces;

/// <summary>
/// Interface for generating embeddings from text.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generates embeddings for the given text.
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates embeddings for multiple texts in batch.
    /// </summary>
    Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
}
