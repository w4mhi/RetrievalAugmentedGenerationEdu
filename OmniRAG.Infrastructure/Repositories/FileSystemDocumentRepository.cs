using Microsoft.Extensions.Logging;
using OmniRAG.Core.Interfaces;
using OmniRAG.Core.Models;
using System.Collections.Concurrent;

namespace OmniRAG.Infrastructure.Repositories;

/// <summary>
/// File system-based implementation of document repository.
/// Repository Pattern: Encapsulates file system access for documents.
/// Single Responsibility Principle: Handles only file system document storage.
/// </summary>
/// <remarks>
/// This implementation:
/// - Scans a base directory for documents
/// - Maintains an in-memory cache for fast metadata queries
/// - Supports lazy loading of document content
/// - Thread-safe for concurrent access
/// </remarks>
public sealed class FileSystemDocumentRepository : IDocumentRepository
{
    private readonly string basePath;
    private readonly ILogger<FileSystemDocumentRepository>? logger;
    private readonly ConcurrentDictionary<string, DocumentMetadata> documentCache;
    private readonly SemaphoreSlim refreshLock;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemDocumentRepository"/> class.
    /// </summary>
    /// <param name="basePath">The base directory path for document storage.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public FileSystemDocumentRepository(string basePath, ILogger<FileSystemDocumentRepository>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);

        this.basePath = Path.GetFullPath(basePath);
        this.logger = logger;
        this.documentCache = new ConcurrentDictionary<string, DocumentMetadata>();
        this.refreshLock = new SemaphoreSlim(1, 1);

        if (!Directory.Exists(this.basePath))
        {
            this.logger?.LogInformation("Creating document repository directory: {BasePath}", this.basePath);
            Directory.CreateDirectory(this.basePath);
        }

        // Initial scan
        this.RefreshAsync().GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentMetadata>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Make async for consistency
        this.logger?.LogDebug("Getting all documents. Cache contains {Count} items", this.documentCache.Count);
        return this.documentCache.Values.ToList();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentMetadata>> GetByExtensionAsync(string extension, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);

        await Task.CompletedTask;

        string normalizedExtension = extension.StartsWith('.') ? extension : $".{extension}";

        List<DocumentMetadata> results = this.documentCache.Values
            .Where(d => d.Extension.Equals(normalizedExtension, StringComparison.OrdinalIgnoreCase))
            .ToList();

        this.logger?.LogDebug("Found {Count} documents with extension {Extension}", results.Count, normalizedExtension);
        return results;
    }

    /// <inheritdoc/>
    public async Task<Document?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (!this.documentCache.TryGetValue(id, out DocumentMetadata? metadata))
        {
            this.logger?.LogDebug("Document not found with ID: {Id}", id);
            return null;
        }

        return await this.LoadDocumentContentAsync(metadata, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Document?> GetByFilePathAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        string normalizedPath = Path.GetFullPath(filePath);
        DocumentMetadata? metadata = this.documentCache.Values.FirstOrDefault(d => 
            d.FilePath.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase));

        if (metadata == null)
        {
            this.logger?.LogDebug("Document not found at path: {FilePath}", normalizedPath);
            return null;
        }

        return await this.LoadDocumentContentAsync(metadata, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<string> AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (this.documentCache.ContainsKey(document.Id))
        {
            throw new InvalidOperationException($"Document with ID '{document.Id}' already exists");
        }

        // If document has content, write it to file system
        if (document.Content != null && document.Content.Length > 0)
        {
            string targetPath = Path.Combine(this.basePath, document.FileName);

            if (File.Exists(targetPath))
            {
                throw new InvalidOperationException($"File already exists at path: {targetPath}");
            }

            await File.WriteAllBytesAsync(targetPath, document.Content, cancellationToken);
            this.logger?.LogInformation("Added document {FileName} ({Size} bytes)", document.FileName, document.Content.Length);
        }

        // Add to cache
        DocumentMetadata metadata = this.CreateMetadata(document);
        this.documentCache.TryAdd(document.Id, metadata);

        return document.Id;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Document document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!this.documentCache.ContainsKey(document.Id))
        {
            throw new InvalidOperationException($"Document with ID '{document.Id}' does not exist");
        }

        // Update file if content provided
        if (document.Content != null && document.Content.Length > 0)
        {
            await File.WriteAllBytesAsync(document.FilePath, document.Content, cancellationToken);
            this.logger?.LogInformation("Updated document {FileName} ({Size} bytes)", document.FileName, document.Content.Length);
        }

        // Update cache
        DocumentMetadata metadata = this.CreateMetadata(document);
        this.documentCache[document.Id] = metadata;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (!this.documentCache.TryRemove(id, out DocumentMetadata? metadata))
        {
            this.logger?.LogDebug("Document not found for deletion: {Id}", id);
            return false;
        }

        if (File.Exists(metadata.FilePath))
        {
            await Task.Run(() => File.Delete(metadata.FilePath), cancellationToken);
            this.logger?.LogInformation("Deleted document: {FileName}", metadata.FileName);
        }

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return this.documentCache.ContainsKey(id);
    }

    /// <inheritdoc/>
    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return this.documentCache.Count;
    }

    /// <inheritdoc/>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await this.refreshLock.WaitAsync(cancellationToken);

        try
        {
            this.logger?.LogDebug("Refreshing document repository from: {BasePath}", this.basePath);

            string[] files = Directory.GetFiles(this.basePath, "*.*", SearchOption.TopDirectoryOnly);

            // Track existing IDs
            HashSet<string> existingIds = new HashSet<string>(this.documentCache.Keys);
            HashSet<string> scannedIds = new HashSet<string>();

            foreach (string filePath in files)
            {
                try
                {
                    FileInfo fileInfo = new FileInfo(filePath);
                    string id = Path.GetFileNameWithoutExtension(filePath);

                    scannedIds.Add(id);

                    // Add or update metadata
                    DocumentMetadata metadata = new DocumentMetadata
                    {
                        Id = id,
                        FilePath = Path.GetFullPath(filePath),
                        FileName = Path.GetFileName(filePath),
                        Size = fileInfo.Length,
                        LastModified = fileInfo.LastWriteTimeUtc,
                        Created = fileInfo.CreationTimeUtc,
                        Extension = fileInfo.Extension,
                        IsIndexed = this.documentCache.TryGetValue(id, out DocumentMetadata? existing) && existing.IsIndexed
                    };

                    this.documentCache[id] = metadata;
                }
                catch (Exception ex)
                {
                    this.logger?.LogWarning(ex, "Error scanning file: {FilePath}", filePath);
                }
            }

            // Remove documents that no longer exist
            IEnumerable<string> removedIds = existingIds.Except(scannedIds);
            foreach (string id in removedIds)
            {
                this.documentCache.TryRemove(id, out _);
                this.logger?.LogDebug("Removed document from cache (file deleted): {Id}", id);
            }

            this.logger?.LogInformation("Repository refreshed: {Count} documents", this.documentCache.Count);
        }
        finally
        {
            this.refreshLock.Release();
        }
    }

    private async Task<Document> LoadDocumentContentAsync(DocumentMetadata metadata, CancellationToken cancellationToken)
    {
        byte[]? content = null;

        if (File.Exists(metadata.FilePath))
        {
            content = await File.ReadAllBytesAsync(metadata.FilePath, cancellationToken);
        }

        return new Document
        {
            Id = metadata.Id,
            FilePath = metadata.FilePath,
            FileName = metadata.FileName,
            Content = content,
            Size = metadata.Size,
            LastModified = metadata.LastModified,
            Created = metadata.Created,
            Extension = metadata.Extension,
            IsIndexed = metadata.IsIndexed
        };
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
