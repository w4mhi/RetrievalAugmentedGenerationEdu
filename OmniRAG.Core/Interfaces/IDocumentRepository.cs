using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OmniRAG.Core.Models;

namespace OmniRAG.Core.Interfaces;

/// <summary>
/// Repository pattern interface for document storage operations.
/// Repository Pattern: Encapsulates data access logic and provides a collection-like interface.
/// Dependency Inversion Principle: High-level modules depend on this abstraction, not concrete implementations.
/// Interface Segregation Principle: Focused interface for document CRUD operations only.
/// </summary>
/// <remarks>
/// Benefits:
/// - Testability: Easy to mock for unit tests
/// - Flexibility: Can swap implementations (file system, blob storage, database)
/// - Maintainability: Centralized data access logic
/// - Single Responsibility: Handles only document storage concerns
/// </remarks>
public interface IDocumentRepository
{
    /// <summary>
    /// Gets all document metadata asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Collection of document metadata.</returns>
    /// <remarks>
    /// Returns metadata only (not full content) for performance.
    /// Use GetByIdAsync to retrieve full document with content.
    /// </remarks>
    Task<IEnumerable<DocumentMetadata>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents filtered by extension.
    /// </summary>
    /// <param name="extension">File extension to filter by (e.g., ".pdf").</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Collection of document metadata matching the extension.</returns>
    Task<IEnumerable<DocumentMetadata>> GetByExtensionAsync(
        string extension, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document by its unique identifier.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The document with full content, or null if not found.</returns>
    Task<Document?> GetByIdAsync(
        string id, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document by its file path.
    /// </summary>
    /// <param name="filePath">The absolute file path.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The document with full content, or null if not found.</returns>
    Task<Document?> GetByFilePathAsync(
        string filePath, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new document to the repository.
    /// </summary>
    /// <param name="document">The document to add.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The document ID of the added document.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if document with same ID already exists.
    /// </exception>
    Task<string> AddAsync(
        Document document, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document in the repository.
    /// </summary>
    /// <param name="document">The document to update.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <exception cref="InvalidOperationException">Thrown if document does not exist.</exception>
    Task UpdateAsync(
        Document document, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document by its identifier.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>True if document was deleted, false if not found.</returns>
    Task<bool> DeleteAsync(
        string id, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a document exists by its identifier.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>True if document exists, false otherwise.</returns>
    Task<bool> ExistsAsync(
        string id, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of documents in the repository.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Total document count.</returns>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the repository (re-scans the underlying storage).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <remarks>
    /// Useful for file system implementations to detect externally added/modified files.
    /// </remarks>
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
