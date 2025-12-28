using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using OmniRAG.Core.Interfaces;
using OmniRAG.Infrastructure.Embeddings;

using Xunit;

namespace OmniRAG.Tests.Infrastructure;

/// <summary>
/// Unit tests for CachedEmbeddingService.
/// Tests caching behavior, decorator pattern, and performance characteristics.
/// </summary>
public class CachedEmbeddingServiceTests : IDisposable
{
    private readonly Mock<IEmbeddingService> mockInnerService;
    private readonly IMemoryCache memoryCache;
    private readonly CachedEmbeddingService cachedService;
    private bool disposed;

    public CachedEmbeddingServiceTests()
    {
        this.mockInnerService = new Mock<IEmbeddingService>();
        
        // Create memory cache with size limit
        MemoryCacheOptions cacheOptions = new MemoryCacheOptions
        {
            SizeLimit = 1024 * 1024 * 100 // 100 MB limit
        };
        this.memoryCache = new MemoryCache(cacheOptions);
        
        this.cachedService = new CachedEmbeddingService(
            this.mockInnerService.Object,
            this.memoryCache,
            TimeSpan.FromHours(1),
            NullLogger<CachedEmbeddingService>.Instance);
    }

    [Fact]
    public void Constructor_WithNullInnerService_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new CachedEmbeddingService(
            null!,
            this.memoryCache,
            null,
            NullLogger<CachedEmbeddingService>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("innerService");
    }

    [Fact]
    public void Constructor_WithNullCache_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new CachedEmbeddingService(
            this.mockInnerService.Object,
            null!,
            null,
            NullLogger<CachedEmbeddingService>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("cache");
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithNullText_ThrowsArgumentException()
    {
        // Arrange & Act
        Func<Task> act = async () => await this.cachedService.GenerateEmbeddingAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("text");
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithEmptyText_ThrowsArgumentException()
    {
        // Arrange & Act
        Func<Task> act = async () => await this.cachedService.GenerateEmbeddingAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("text");
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_FirstCall_CallsInnerServiceAndCachesResult()
    {
        // Arrange
        string testText = "This is a test document about machine learning.";
        float[] expectedEmbedding = new float[] { 0.1f, 0.2f, 0.3f, 0.4f };
        
        this.mockInnerService
            .Setup(x => x.GenerateEmbeddingAsync(testText, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEmbedding);

        // Act
        float[] result = await this.cachedService.GenerateEmbeddingAsync(testText);

        // Assert
        result.Should().BeEquivalentTo(expectedEmbedding);
        this.mockInnerService.Verify(
            x => x.GenerateEmbeddingAsync(testText, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_SecondCallWithSameText_ReturnsCachedResult()
    {
        // Arrange
        string testText = "This is a test document about machine learning.";
        float[] expectedEmbedding = new float[] { 0.1f, 0.2f, 0.3f, 0.4f };
        
        this.mockInnerService
            .Setup(x => x.GenerateEmbeddingAsync(testText, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEmbedding);

        // Act - First call
        float[] firstResult = await this.cachedService.GenerateEmbeddingAsync(testText);
        
        // Act - Second call (should hit cache)
        float[] secondResult = await this.cachedService.GenerateEmbeddingAsync(testText);

        // Assert
        firstResult.Should().BeEquivalentTo(expectedEmbedding);
        secondResult.Should().BeEquivalentTo(expectedEmbedding);
        secondResult.Should().BeSameAs(firstResult); // Same instance from cache
        
        // Inner service should only be called once
        this.mockInnerService.Verify(
            x => x.GenerateEmbeddingAsync(testText, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_DifferentTexts_CallsInnerServiceForEach()
    {
        // Arrange
        string text1 = "First document";
        string text2 = "Second document";
        float[] embedding1 = new float[] { 0.1f, 0.2f };
        float[] embedding2 = new float[] { 0.3f, 0.4f };
        
        this.mockInnerService
            .Setup(x => x.GenerateEmbeddingAsync(text1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding1);
        
        this.mockInnerService
            .Setup(x => x.GenerateEmbeddingAsync(text2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding2);

        // Act
        float[] result1 = await this.cachedService.GenerateEmbeddingAsync(text1);
        float[] result2 = await this.cachedService.GenerateEmbeddingAsync(text2);

        // Assert
        result1.Should().BeEquivalentTo(embedding1);
        result2.Should().BeEquivalentTo(embedding2);
        
        this.mockInnerService.Verify(
            x => x.GenerateEmbeddingAsync(text1, It.IsAny<CancellationToken>()),
            Times.Once);
        this.mockInnerService.Verify(
            x => x.GenerateEmbeddingAsync(text2, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithNullTexts_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Func<Task> act = async () => await this.cachedService.GenerateEmbeddingsAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithEmptyList_ReturnsEmptyArray()
    {
        // Arrange
        List<string> emptyTexts = new List<string>();

        // Act
        IReadOnlyList<float[]> result = await this.cachedService.GenerateEmbeddingsAsync(emptyTexts);

        // Assert
        result.Should().BeEmpty();
        this.mockInnerService.Verify(
            x => x.GenerateEmbeddingsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_AllUncached_CallsInnerServiceForAll()
    {
        // Arrange
        List<string> texts = new List<string> { "Text 1", "Text 2", "Text 3" };
        List<float[]> embeddings = new List<float[]>
        {
            new float[] { 0.1f, 0.2f },
            new float[] { 0.3f, 0.4f },
            new float[] { 0.5f, 0.6f }
        };
        
        this.mockInnerService
            .Setup(x => x.GenerateEmbeddingsAsync(
                It.Is<IEnumerable<string>>(t => t.SequenceEqual(texts)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddings);

        // Act
        IReadOnlyList<float[]> result = await this.cachedService.GenerateEmbeddingsAsync(texts);

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(embeddings);
        this.mockInnerService.Verify(
            x => x.GenerateEmbeddingsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_SomeCached_OnlyCallsInnerServiceForUncached()
    {
        // Arrange
        string text1 = "Cached text";
        string text2 = "Uncached text 1";
        string text3 = "Uncached text 2";
        
        float[] cachedEmbedding = new float[] { 0.1f, 0.2f };
        float[] uncachedEmbedding1 = new float[] { 0.3f, 0.4f };
        float[] uncachedEmbedding2 = new float[] { 0.5f, 0.6f };

        // Pre-cache text1
        this.mockInnerService
            .Setup(x => x.GenerateEmbeddingAsync(text1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedEmbedding);
        await this.cachedService.GenerateEmbeddingAsync(text1);

        // Setup for batch call
        this.mockInnerService
            .Setup(x => x.GenerateEmbeddingsAsync(
                It.Is<IEnumerable<string>>(t => t.SequenceEqual(new[] { text2, text3 })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<float[]> { uncachedEmbedding1, uncachedEmbedding2 });

        // Act
        List<string> batchTexts = new List<string> { text1, text2, text3 };
        IReadOnlyList<float[]> results = await this.cachedService.GenerateEmbeddingsAsync(batchTexts);

        // Assert
        results.Should().HaveCount(3);
        results[0].Should().BeEquivalentTo(cachedEmbedding);
        results[1].Should().BeEquivalentTo(uncachedEmbedding1);
        results[2].Should().BeEquivalentTo(uncachedEmbedding2);
        
        // Should only call batch method once for the uncached items
        this.mockInnerService.Verify(
            x => x.GenerateEmbeddingsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_AllCached_DoesNotCallInnerService()
    {
        // Arrange
        List<string> texts = new List<string> { "Text 1", "Text 2" };
        float[] embedding1 = new float[] { 0.1f, 0.2f };
        float[] embedding2 = new float[] { 0.3f, 0.4f };

        // Pre-cache all texts
        this.mockInnerService
            .Setup(x => x.GenerateEmbeddingAsync(texts[0], It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding1);
        this.mockInnerService
            .Setup(x => x.GenerateEmbeddingAsync(texts[1], It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding2);
        
        await this.cachedService.GenerateEmbeddingAsync(texts[0]);
        await this.cachedService.GenerateEmbeddingAsync(texts[1]);

        // Reset mock to verify batch call
        this.mockInnerService.Reset();

        // Act
        IReadOnlyList<float[]> results = await this.cachedService.GenerateEmbeddingsAsync(texts);

        // Assert
        results.Should().HaveCount(2);
        results[0].Should().BeEquivalentTo(embedding1);
        results[1].Should().BeEquivalentTo(embedding2);
        
        // Should not call inner service at all
        this.mockInnerService.Verify(
            x => x.GenerateEmbeddingsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CacheExpiration_AfterTimeout_RegeneratesEmbedding()
    {
        // Arrange
        string testText = "Test text";
        float[] embedding = new float[] { 0.1f, 0.2f };
        
        // Create service with very short expiration (100ms)
        CachedEmbeddingService shortCacheService = new CachedEmbeddingService(
            this.mockInnerService.Object,
            this.memoryCache,
            TimeSpan.FromMilliseconds(100),
            NullLogger<CachedEmbeddingService>.Instance);

        this.mockInnerService
            .Setup(x => x.GenerateEmbeddingAsync(testText, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        // Act - First call
        await shortCacheService.GenerateEmbeddingAsync(testText);
        
        // Wait for cache to expire
        await Task.Delay(150);
        
        // Second call after expiration
        await shortCacheService.GenerateEmbeddingAsync(testText);

        // Assert - Should call inner service twice
        this.mockInnerService.Verify(
            x => x.GenerateEmbeddingAsync(testText, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                this.cachedService?.Dispose();
                this.memoryCache?.Dispose();
            }

            this.disposed = true;
        }
    }
}
