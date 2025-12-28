using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using OmniRAG.Core.Interfaces;
using OmniRAG.Core.Models;

namespace OmniRAG.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of document repository for testing.
/// Repository Pattern: Provides a test double for unit testing without file system dependencies.
/// </summary>
/// <remarks>
/// Benefits:
/// - Fast: No disk I/O
/// - Isolated: No side effects on file system
/// - Deterministic: Consistent test behavior
/// - Thread-safe: Uses ConcurrentDictionary
/// 
/// Use this for:
/// - Unit tests
/// - Integration tests that don't require persistence
/// - Development/prototyping
/// </remarks>
public sealed class InMemoryDocumentRepository : IDocumentRepository
{
    private readonly ConcurrentDictionary<string, Document> documents;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryDocumentRepository"/> class.
    /// </summary>
    public InMemoryDocumentRepository()
    {
        this.documents = new ConcurrentDictionary<string, Document>();
    }

    /// <summary>
    /// Initializes a new instance with pre-populated documents (for testing).
    /// </summary>
    /// <param name="initialDocuments">Initial documents to populate the repository.</param>
    public InMemoryDocumentRepository(IEnumerable<Document> initialDocuments)
    {
        this.documents = new ConcurrentDictionary<string, Document>(
            initialDocuments.ToDictionary(d => d.Id));
    }

    /// <inheritdoc/>
    public Task<IEnumerable<DocumentMetadata>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        List<DocumentMetadata> metadata = this.documents.Values
            .Select(this.CreateMetadata)
            .ToList();

        return Task.FromResult<IEnumerable<DocumentMetadata>>(metadata);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<DocumentMetadata>> GetByExtensionAsync(string extension, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);

        string normalizedExtension = extension.StartsWith('.') ? extension : $".{extension}";

        List<DocumentMetadata> metadata = this.documents.Values
            .Where(d => d.Extension.Equals(normalizedExtension, StringComparison.OrdinalIgnoreCase))
            .Select(this.CreateMetadata)
            .ToList();

        return Task.FromResult<IEnumerable<DocumentMetadata>>(metadata);
    }

    /// <inheritdoc/>
    public Task<Document?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        this.documents.TryGetValue(id, out Document? document);
        return Task.FromResult(document);
    }

    /// <inheritdoc/>
    public Task<Document?> GetByFilePathAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        string normalizedPath = Path.GetFullPath(filePath);
        Document? document = this.documents.Values.FirstOrDefault(d =>
            d.FilePath.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(document);
    }

    /// <inheritdoc/>
    public Task<string> AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!this.documents.TryAdd(document.Id, document))
        {
            throw new InvalidOperationException($"Document with ID '{document.Id}' already exists");
        }

        return Task.FromResult(document.Id);
    }

    /// <inheritdoc/>
    public Task UpdateAsync(Document document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!this.documents.ContainsKey(document.Id))
        {
            throw new InvalidOperationException($"Document with ID '{document.Id}' does not exist");
        }

        this.documents[document.Id] = document;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        bool removed = this.documents.TryRemove(id, out _);
        return Task.FromResult(removed);
    }

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        bool exists = this.documents.ContainsKey(id);
        return Task.FromResult(exists);
    }

    /// <inheritdoc/>
    public Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(this.documents.Count);
    }

    /// <inheritdoc/>
    public Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        // No-op for in-memory implementation
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all documents from the repository (useful for test cleanup).
    /// </summary>
    public void Clear()
    {
        this.documents.Clear();
    }

    private DocumentMetadata CreateMetadata(Document document)
    {
        return new DocumentMetadata
        {
            Id = document.Id,
            FilePath = document.FilePath,
            FileName = document.FileName,
            Size = document.Size,
            LastModified = document.LastModified,
            Created = document.Created,
            Extension = document.Extension,
            IsIndexed = document.IsIndexed
        };
    }
}
