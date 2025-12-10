using OmniRAG.Core.Models;

namespace OmniRAG.Core.Interfaces;

/// <summary>
/// Interface for document loading and processing.
/// Interface Segregation Principle: Focused on document loading only.
/// </summary>
public interface IDocumentLoader
{
    /// <summary>
    /// Loads and processes PDF documents from the specified directory.
    /// </summary>
    Task<IReadOnlyList<DocumentChunk>> LoadDocumentsAsync(string directoryPath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Loads and processes a single PDF document.
    /// </summary>
    Task<IReadOnlyList<DocumentChunk>> LoadDocumentAsync(string filePath, CancellationToken cancellationToken = default);
}
