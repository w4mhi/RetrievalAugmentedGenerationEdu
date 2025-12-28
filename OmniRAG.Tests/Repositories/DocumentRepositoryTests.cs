using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using OmniRAG.Core.Models;
using OmniRAG.Infrastructure.Repositories;

using Xunit;

namespace OmniRAG.Tests.Repositories;

/// <summary>
/// Unit tests for document repository implementations.
/// Tests both InMemoryDocumentRepository and FileSystemDocumentRepository.
/// </summary>
public sealed class DocumentRepositoryTests : IDisposable
{
    private readonly string testDirectory;

    public DocumentRepositoryTests()
    {
        // Create a unique test directory for each test run
        this.testDirectory = Path.Combine(Path.GetTempPath(), $"OmniRAG_Tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(this.testDirectory);
    }

    public void Dispose()
    {
        // Cleanup test directory
        if (Directory.Exists(this.testDirectory))
        {
            Directory.Delete(this.testDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task InMemory_AddAsync_AddsDocumentSuccessfully()
    {
        // Arrange
        InMemoryDocumentRepository repository = new InMemoryDocumentRepository();
        Document document = this.CreateTestDocument("test-doc-1", "test.pdf");

        // Act
        string id = await repository.AddAsync(document);

        // Assert
        id.Should().Be("test-doc-1");
        Document? retrieved = await repository.GetByIdAsync(id);
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be("test-doc-1");
        retrieved.FileName.Should().Be("test.pdf");
    }

    [Fact]
    public async Task InMemory_AddAsync_WithDuplicateId_ThrowsException()
    {
        // Arrange
        InMemoryDocumentRepository repository = new InMemoryDocumentRepository();
        Document document1 = this.CreateTestDocument("duplicate-id", "test1.pdf");
        Document document2 = this.CreateTestDocument("duplicate-id", "test2.pdf");

        await repository.AddAsync(document1);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.AddAsync(document2));
    }

    [Fact]
    public async Task InMemory_GetAllAsync_ReturnsAllDocuments()
    {
        // Arrange
        Document[] documents = new[]
        {
            this.CreateTestDocument("doc-1", "test1.pdf"),
            this.CreateTestDocument("doc-2", "test2.pdf"),
            this.CreateTestDocument("doc-3", "test3.docx")
        };
        InMemoryDocumentRepository repository = new InMemoryDocumentRepository(documents);

        // Act
        IEnumerable<DocumentMetadata> all = await repository.GetAllAsync();

        // Assert
        all.Should().HaveCount(3);
        all.Select(d => d.Id).Should().Contain(new[] { "doc-1", "doc-2", "doc-3" });
    }

    [Fact]
    public async Task InMemory_GetByExtensionAsync_FiltersCorrectly()
    {
        // Arrange
        Document[] documents = new[]
        {
            this.CreateTestDocument("doc-1", "test1.pdf", extension: ".pdf"),
            this.CreateTestDocument("doc-2", "test2.pdf", extension: ".pdf"),
            this.CreateTestDocument("doc-3", "test3.docx", extension: ".docx")
        };
        InMemoryDocumentRepository repository = new InMemoryDocumentRepository(documents);

        // Act
        IEnumerable<DocumentMetadata> pdfDocs = await repository.GetByExtensionAsync(".pdf");

        // Assert
        pdfDocs.Should().HaveCount(2);
        pdfDocs.Select(d => d.Id).Should().Contain(new[] { "doc-1", "doc-2" });
    }

    [Fact]
    public async Task InMemory_UpdateAsync_UpdatesDocumentSuccessfully()
    {
        // Arrange
        InMemoryDocumentRepository repository = new InMemoryDocumentRepository();
        Document document = this.CreateTestDocument("update-test", "original.pdf");
        await repository.AddAsync(document);

        Document updated = new Document
        {
            Id = document.Id,
            FilePath = document.FilePath,
            FileName = "updated.pdf",
            Content = document.Content,
            Size = 5000,
            LastModified = document.LastModified,
            Created = document.Created,
            Extension = document.Extension,
            IsIndexed = document.IsIndexed
        };

        // Act
        await repository.UpdateAsync(updated);

        // Assert
        Document? retrieved = await repository.GetByIdAsync("update-test");
        retrieved.Should().NotBeNull();
        retrieved!.FileName.Should().Be("updated.pdf");
        retrieved.Size.Should().Be(5000);
    }

    [Fact]
    public async Task InMemory_UpdateAsync_NonExistentDocument_ThrowsException()
    {
        // Arrange
        InMemoryDocumentRepository repository = new InMemoryDocumentRepository();
        Document document = this.CreateTestDocument("non-existent", "test.pdf");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateAsync(document));
    }

    [Fact]
    public async Task InMemory_DeleteAsync_RemovesDocumentSuccessfully()
    {
        // Arrange
        InMemoryDocumentRepository repository = new InMemoryDocumentRepository();
        Document document = this.CreateTestDocument("delete-test", "test.pdf");
        await repository.AddAsync(document);

        // Act
        bool deleted = await repository.DeleteAsync("delete-test");

        // Assert
        deleted.Should().BeTrue();
        Document? retrieved = await repository.GetByIdAsync("delete-test");
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task InMemory_DeleteAsync_NonExistentDocument_ReturnsFalse()
    {
        // Arrange
        InMemoryDocumentRepository repository = new InMemoryDocumentRepository();

        // Act
        bool deleted = await repository.DeleteAsync("non-existent");

        // Assert
        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task InMemory_ExistsAsync_ReturnsCorrectResult()
    {
        // Arrange
        InMemoryDocumentRepository repository = new InMemoryDocumentRepository();
        Document document = this.CreateTestDocument("exists-test", "test.pdf");
        await repository.AddAsync(document);

        // Act & Assert
        (await repository.ExistsAsync("exists-test")).Should().BeTrue();
        (await repository.ExistsAsync("non-existent")).Should().BeFalse();
    }

    [Fact]
    public async Task InMemory_GetCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        Document[] documents = new[]
        {
            this.CreateTestDocument("doc-1", "test1.pdf"),
            this.CreateTestDocument("doc-2", "test2.pdf"),
            this.CreateTestDocument("doc-3", "test3.pdf")
        };
        InMemoryDocumentRepository repository = new InMemoryDocumentRepository(documents);

        // Act
        int count = await repository.GetCountAsync();

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public void FileSystem_Constructor_CreatesDirectoryIfNotExists()
    {
        // Arrange
        string newPath = Path.Combine(this.testDirectory, "new-dir");

        // Act
        FileSystemDocumentRepository repository = new FileSystemDocumentRepository(newPath);

        // Assert
        Directory.Exists(newPath).Should().BeTrue();
    }

    [Fact]
    public async Task FileSystem_RefreshAsync_ScansExistingFiles()
    {
        // Arrange
        string testFile1 = Path.Combine(this.testDirectory, "test1.pdf");
        string testFile2 = Path.Combine(this.testDirectory, "test2.pdf");

        await File.WriteAllBytesAsync(testFile1, new byte[] { 1, 2, 3 });
        await File.WriteAllBytesAsync(testFile2, new byte[] { 4, 5, 6 });

        // Act
        FileSystemDocumentRepository repository = new FileSystemDocumentRepository(this.testDirectory);
        IEnumerable<DocumentMetadata> documents = await repository.GetAllAsync();

        // Assert
        documents.Should().HaveCount(2);
        documents.Select(d => d.FileName).Should().Contain(new[] { "test1.pdf", "test2.pdf" });
    }

    [Fact]
    public async Task FileSystem_AddAsync_WritesFileToFileSystem()
    {
        // Arrange
        FileSystemDocumentRepository repository = new FileSystemDocumentRepository(this.testDirectory);
        Document document = new Document
        {
            Id = "add-test",
            FilePath = Path.Combine(this.testDirectory, "add-test.pdf"),
            FileName = "add-test.pdf",
            Content = new byte[] { 1, 2, 3, 4, 5 },
            Size = 5,
            LastModified = DateTime.UtcNow,
            Created = DateTime.UtcNow,
            Extension = ".pdf",
            IsIndexed = false
        };

        // Act
        string id = await repository.AddAsync(document);

        // Assert
        id.Should().Be("add-test");
        File.Exists(Path.Combine(this.testDirectory, "add-test.pdf")).Should().BeTrue();
        byte[] fileContent = await File.ReadAllBytesAsync(Path.Combine(this.testDirectory, "add-test.pdf"));
        fileContent.Should().Equal(new byte[] { 1, 2, 3, 4, 5 });
    }

    [Fact]
    public async Task FileSystem_GetByIdAsync_LoadsContentLazily()
    {
        // Arrange
        string testFile = Path.Combine(this.testDirectory, "lazy-load.pdf");
        await File.WriteAllBytesAsync(testFile, new byte[] { 10, 20, 30 });

        FileSystemDocumentRepository repository = new FileSystemDocumentRepository(this.testDirectory);

        // Act
        Document? document = await repository.GetByIdAsync("lazy-load");

        // Assert
        document.Should().NotBeNull();
        document!.Content.Should().NotBeNull();
        document.Content.Should().Equal(new byte[] { 10, 20, 30 });
    }

    [Fact]
    public async Task FileSystem_GetByExtensionAsync_FiltersCorrectly()
    {
        // Arrange
        await File.WriteAllBytesAsync(Path.Combine(this.testDirectory, "doc1.pdf"), new byte[] { 1 });
        await File.WriteAllBytesAsync(Path.Combine(this.testDirectory, "doc2.pdf"), new byte[] { 2 });
        await File.WriteAllBytesAsync(Path.Combine(this.testDirectory, "doc3.docx"), new byte[] { 3 });

        FileSystemDocumentRepository repository = new FileSystemDocumentRepository(this.testDirectory);

        // Act
        IEnumerable<DocumentMetadata> pdfDocs = await repository.GetByExtensionAsync(".pdf");

        // Assert
        pdfDocs.Should().HaveCount(2);
        pdfDocs.Select(d => d.Extension).Should().AllBe(".pdf");
    }

    [Fact]
    public async Task FileSystem_DeleteAsync_RemovesFileFromFileSystem()
    {
        // Arrange
        string testFile = Path.Combine(this.testDirectory, "delete-test.pdf");
        await File.WriteAllBytesAsync(testFile, new byte[] { 1, 2, 3 });

        FileSystemDocumentRepository repository = new FileSystemDocumentRepository(this.testDirectory);

        // Act
        bool deleted = await repository.DeleteAsync("delete-test");

        // Assert
        deleted.Should().BeTrue();
        File.Exists(testFile).Should().BeFalse();
    }

    [Fact]
    public async Task FileSystem_RefreshAsync_DetectsNewFiles()
    {
        // Arrange
        FileSystemDocumentRepository repository = new FileSystemDocumentRepository(this.testDirectory);
        int initialCount = await repository.GetCountAsync();

        // Act - Add file externally
        string newFile = Path.Combine(this.testDirectory, "external-add.pdf");
        await File.WriteAllBytesAsync(newFile, new byte[] { 1, 2, 3 });
        await repository.RefreshAsync();

        // Assert
        int newCount = await repository.GetCountAsync();
        newCount.Should().Be(initialCount + 1);
    }

    [Fact]
    public async Task FileSystem_RefreshAsync_DetectsDeletedFiles()
    {
        // Arrange
        string testFile = Path.Combine(this.testDirectory, "external-delete.pdf");
        await File.WriteAllBytesAsync(testFile, new byte[] { 1, 2, 3 });

        FileSystemDocumentRepository repository = new FileSystemDocumentRepository(this.testDirectory);
        int initialCount = await repository.GetCountAsync();

        // Act - Delete file externally
        File.Delete(testFile);
        await repository.RefreshAsync();

        // Assert
        int newCount = await repository.GetCountAsync();
        newCount.Should().Be(initialCount - 1);
    }

    [Fact]
    public async Task FileSystem_GetByFilePathAsync_FindsDocumentByPath()
    {
        // Arrange
        string testFile = Path.Combine(this.testDirectory, "path-test.pdf");
        await File.WriteAllBytesAsync(testFile, new byte[] { 1, 2, 3 });

        FileSystemDocumentRepository repository = new FileSystemDocumentRepository(this.testDirectory);

        // Act
        Document? document = await repository.GetByFilePathAsync(testFile);

        // Assert
        document.Should().NotBeNull();
        document!.FilePath.Should().Be(Path.GetFullPath(testFile));
    }

    private Document CreateTestDocument(
        string id,
        string fileName,
        byte[]? content = null,
        string extension = ".pdf")
    {
        return new Document
        {
            Id = id,
            FilePath = Path.Combine(this.testDirectory, fileName),
            FileName = fileName,
            Content = content ?? new byte[] { 1, 2, 3 },
            Size = content?.Length ?? 3,
            LastModified = DateTime.UtcNow,
            Created = DateTime.UtcNow,
            Extension = extension,
            IsIndexed = false
        };
    }
}
