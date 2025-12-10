using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OmniRAG.Core.Interfaces;
using OmniRAG.Core.Models;
using OmniRAG.Core.Services;

namespace OmniRAG.Tests.Core;

/// <summary>
/// Unit tests for RagEngine service.
/// Testing with mocks to isolate dependencies.
/// </summary>
public class RagEngineTests
{
    private readonly Mock<IDocumentLoader> mockDocumentLoader;
    private readonly Mock<IEmbeddingService> mockEmbeddingService;
    private readonly Mock<IVectorStore> mockVectorStore;
    private readonly Mock<ILogger<RagEngine>> mockLogger;
    private readonly RagEngine ragEngine;

    public RagEngineTests()
    {
        this.mockDocumentLoader = new Mock<IDocumentLoader>();
        this.mockEmbeddingService = new Mock<IEmbeddingService>();
        this.mockVectorStore = new Mock<IVectorStore>();
        this.mockLogger = new Mock<ILogger<RagEngine>>();
        this.ragEngine = new RagEngine(
            this.mockDocumentLoader.Object,
            this.mockEmbeddingService.Object,
            this.mockVectorStore.Object,
            logger: this.mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullDocumentLoader_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new RagEngine(null!, this.mockEmbeddingService.Object, this.mockVectorStore.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("documentLoader");
    }

    [Fact]
    public async Task QueryAsync_WithValidQuery_ShouldReturnResponse()
    {
        // Arrange
        string query = "test query";
        float[] queryEmbedding = new float[] { 0.1f, 0.2f };
        List<SearchResult> searchResults = new List<SearchResult>
        {
            SearchResult.Create(
                DocumentChunk.Create("content", "file.pdf", 1, "section", new float[] { 0.1f }),
                0.95f,
                1)
        };

        this.mockEmbeddingService
            .Setup(x => x.GenerateEmbeddingAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryEmbedding);

        this.mockVectorStore
            .Setup(x => x.SearchAsync(
                queryEmbedding, 
                It.IsAny<RetrievalOptions>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        // Act
        RagResponse response = await this.ragEngine.QueryAsync(query);

        // Assert
        response.Should().NotBeNull();
        response.Query.Should().Be(query);
        response.Sources.Should().HaveCount(1);
        response.Answer.Should().NotBeNullOrEmpty();
        response.ProcessingTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task QueryAsync_WithInvalidQuery_ShouldThrowArgumentException(string? invalidQuery)
    {
        // Act
        Func<Task> act = async () => await this.ragEngine.QueryAsync(invalidQuery!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task IndexDocumentsAsync_WithValidDirectory_ShouldIndexDocuments()
    {
        // Arrange
        string directory = "test_directory";
        List<DocumentChunk> chunks = new List<DocumentChunk>
        {
            DocumentChunk.Create("content1", "file1.pdf", 1, "section", new float[] { 0.1f }),
            DocumentChunk.Create("content2", "file2.pdf", 1, "section", new float[] { 0.2f })
        };

        // Create a temporary directory
        Directory.CreateDirectory(directory);

        try
        {
            this.mockDocumentLoader
                .Setup(x => x.LoadDocumentsAsync(directory, It.IsAny<CancellationToken>()))
                .ReturnsAsync(chunks);

            this.mockVectorStore
                .Setup(x => x.StoreChunksAsync(chunks, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await this.ragEngine.IndexDocumentsAsync(directory);

            // Assert
            this.mockDocumentLoader.Verify(x => x.LoadDocumentsAsync(directory, It.IsAny<CancellationToken>()), Times.Once);
            this.mockVectorStore.Verify(x => x.StoreChunksAsync(chunks, It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            Directory.Delete(directory);
        }
    }

    [Fact]
    public async Task GetIndexStatsAsync_ShouldReturnStats()
    {
        // Arrange
        this.mockVectorStore
            .Setup(x => x.GetChunkCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        // Act
        (int totalChunks, DateTime? lastIndexed) = await this.ragEngine.GetIndexStatsAsync();

        // Assert
        totalChunks.Should().Be(42);
    }
}
