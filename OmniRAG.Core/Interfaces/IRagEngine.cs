using OmniRAG.Core.Models;

namespace OmniRAG.Core.Interfaces;

/// <summary>
/// Interface for RAG engine that orchestrates retrieval and generation.
/// </summary>
public interface IRagEngine
{
    /// <summary>
    /// Processes a query and generates a response using RAG.
    /// </summary>
    Task<RagResponse> QueryAsync(string query, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Indexes documents from the specified directory.
    /// </summary>
    Task IndexDocumentsAsync(string directoryPath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets indexing statistics.
    /// </summary>
    Task<(int TotalChunks, DateTime? LastIndexed)> GetIndexStatsAsync(CancellationToken cancellationToken = default);
}
