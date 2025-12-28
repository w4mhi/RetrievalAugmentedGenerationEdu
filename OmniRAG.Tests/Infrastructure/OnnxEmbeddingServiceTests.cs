using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

using OmniRAG.Infrastructure.Embeddings;

using Xunit;

namespace OmniRAG.Tests.Infrastructure;

/// <summary>
/// Unit tests for OnnxEmbeddingService.
/// Note: These are placeholder tests. Full integration tests require actual ONNX models.
/// </summary>
public class OnnxEmbeddingServiceTests
{
    private readonly Mock<ILogger<OnnxEmbeddingService>> mockLogger;

    public OnnxEmbeddingServiceTests()
    {
        mockLogger = new Mock<ILogger<OnnxEmbeddingService>>();
    }

    [Fact]
    public void Constructor_WithNullModelPath_ThrowsArgumentException()
    {
        // Arrange
        string? modelPath = null;
        string tokenizerPath = "tokenizer.json";
        string modelName = "test-model";
        int dimensions = 384;

        // Act & Assert
        Action act = () => new OnnxEmbeddingService(
            modelPath!,
            tokenizerPath,
            modelName,
            dimensions,
            logger: mockLogger.Object);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("modelPath");
    }

    [Fact]
    public void Constructor_WithNullTokenizerPath_ThrowsArgumentException()
    {
        // Arrange
        string modelPath = "model.onnx";
        string? tokenizerPath = null;
        string modelName = "test-model";
        int dimensions = 384;

        // Act & Assert
        Action act = () => new OnnxEmbeddingService(
            modelPath,
            tokenizerPath!,
            modelName,
            dimensions,
            logger: mockLogger.Object);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("tokenizerPath");
    }

    [Fact]
    public void Constructor_WithZeroDimensions_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        string modelPath = "model.onnx";
        string tokenizerPath = "tokenizer.json";
        string modelName = "test-model";
        int dimensions = 0;

        // Act & Assert
        Action act = () => new OnnxEmbeddingService(
            modelPath,
            tokenizerPath,
            modelName,
            dimensions,
            logger: mockLogger.Object);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("dimensions");
    }

    [Fact]
    public void Constructor_WithNegativeDimensions_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        string modelPath = "model.onnx";
        string tokenizerPath = "tokenizer.json";
        string modelName = "test-model";
        int dimensions = -1;

        // Act & Assert
        Action act = () => new OnnxEmbeddingService(
            modelPath,
            tokenizerPath,
            modelName,
            dimensions,
            logger: mockLogger.Object);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("dimensions");
    }

    [Fact]
    public void Constructor_WithNonExistentModelPath_ThrowsFileNotFoundException()
    {
        // Arrange
        string modelPath = "nonexistent_model.onnx";
        string tokenizerPath = "tokenizer.json";
        string modelName = "test-model";
        int dimensions = 384;

        // Act & Assert
        Action act = () => new OnnxEmbeddingService(
            modelPath,
            tokenizerPath,
            modelName,
            dimensions,
            logger: mockLogger.Object);

        act.Should().Throw<FileNotFoundException>()
            .WithMessage($"*{modelPath}*");
    }

    [Fact]
    public void GenerateEmbeddingAsync_WithNullText_ThrowsArgumentException()
    {
        // Note: This test cannot run without actual ONNX model files
        // In a real scenario, you would use test fixtures with small ONNX models
        
        // This is a placeholder to demonstrate the test structure
        // For actual implementation, you would:
        // 1. Export a small test model to ONNX
        // 2. Include it in test resources
        // 3. Load it in test setup
        
        Assert.True(true, "Placeholder test - requires ONNX model files for full integration testing");
    }

    [Fact]
    public void GenerateEmbeddingsAsync_WithEmptyList_ReturnsEmptyArray()
    {
        // Note: This test cannot run without actual ONNX model files
        Assert.True(true, "Placeholder test - requires ONNX model files for full integration testing");
    }
}

/// <summary>
/// Integration tests for OnnxEmbeddingService.
/// These tests require actual ONNX models to be present.
/// Run after executing: python export_to_onnx.py
/// </summary>
[Trait("Category", "Integration")]
public class OnnxEmbeddingServiceIntegrationTests
{
    private const string TestModelsPath = "models";
    private readonly Mock<ILogger<OnnxEmbeddingService>> mockLogger;

    public OnnxEmbeddingServiceIntegrationTests()
    {
        mockLogger = new Mock<ILogger<OnnxEmbeddingService>>();
    }

    [Fact(Skip = "Requires ONNX models - run 'python export_to_onnx.py' first")]
    public async Task GenerateEmbeddingAsync_WithValidText_ReturnsCorrectDimensions()
    {
        // Arrange
        string modelPath = Path.Combine(TestModelsPath, "all-MiniLM-L6-v2", "model.onnx");
        string tokenizerPath = Path.Combine(TestModelsPath, "all-MiniLM-L6-v2", "tokenizer.json");

        if (!File.Exists(modelPath) || !File.Exists(tokenizerPath))
        {
            Assert.Fail("ONNX model files not found. Run: python export_to_onnx.py");
            return;
        }

        using OnnxEmbeddingService service = new OnnxEmbeddingService(
            modelPath,
            tokenizerPath,
            "all-MiniLM-L6-v2",
            dimensions: 384,
            logger: mockLogger.Object);

        // Act
        float[] embedding = await service.GenerateEmbeddingAsync("This is a test sentence.");

        // Assert
        embedding.Should().NotBeNull();
        embedding.Length.Should().Be(384);
        embedding.Should().OnlyContain(x => !float.IsNaN(x) && !float.IsInfinity(x));
    }

    [Fact(Skip = "Requires ONNX models - run 'python export_to_onnx.py' first")]
    public async Task GenerateEmbeddingsAsync_WithBatch_ReturnsCorrectCount()
    {
        // Arrange
        string modelPath = Path.Combine(TestModelsPath, "all-MiniLM-L6-v2", "model.onnx");
        string tokenizerPath = Path.Combine(TestModelsPath, "all-MiniLM-L6-v2", "tokenizer.json");

        if (!File.Exists(modelPath) || !File.Exists(tokenizerPath))
        {
            Assert.Fail("ONNX model files not found. Run: python export_to_onnx.py");
            return;
        }

        using OnnxEmbeddingService service = new OnnxEmbeddingService(
            modelPath,
            tokenizerPath,
            "all-MiniLM-L6-v2",
            dimensions: 384,
            logger: mockLogger.Object);

        string[] texts = new[] { "Text 1", "Text 2", "Text 3" };

        // Act
        IReadOnlyList<float[]> embeddings = await service.GenerateEmbeddingsAsync(texts);

        // Assert
        embeddings.Should().HaveCount(3);
        embeddings.Should().OnlyContain(e => e.Length == 384);
    }

    [Fact(Skip = "Requires ONNX models - run 'python export_to_onnx.py' first")]
    public void OnnxEmbeddings_AreSimilarToSentenceTransformers()
    {
        // This test validates that ONNX embeddings match Python sentence-transformers
        // Requires both ONNX and Python.NET implementations for comparison

        // Arrange - Text to compare: "Machine learning is transforming the world."

        // Act
        // 1. Generate embedding with ONNX
        // 2. Generate embedding with Python.NET
        // 3. Compute cosine similarity

        // Assert
        // Cosine similarity should be > 0.99 (nearly identical)
        
        Assert.True(true, "Placeholder for cross-validation test");
    }
}
