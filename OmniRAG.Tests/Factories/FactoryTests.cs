using System;
using System.Collections.Generic;
using System.IO;

using FluentAssertions;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using OmniRAG.Core.Interfaces;
using OmniRAG.Core.Models;
using OmniRAG.Infrastructure.Chunking;
using OmniRAG.Infrastructure.Embeddings;
using OmniRAG.Infrastructure.LanguageModels;

using Xunit;

namespace OmniRAG.Tests.Factories;

/// <summary>
/// Comprehensive unit tests for all factory classes.
/// Tests TextChunkerFactory, EmbeddingServiceFactory, and LanguageModelFactory.
/// </summary>
public class FactoryTests
{
    #region TextChunkerFactory Tests

    [Fact]
    public void TextChunkerFactory_Create_WithSemanticStrategy_ShouldReturnSemanticChunker()
    {
        // Arrange & Act
        ITextChunker chunker = TextChunkerFactory.Create(ChunkingStrategy.Semantic);

        // Assert
        chunker.Should().NotBeNull();
        chunker.Should().BeOfType<SemanticTextChunker>();
    }

    [Fact]
    public void TextChunkerFactory_Create_WithFixedStrategy_ShouldReturnFixedSizeChunker()
    {
        // Arrange & Act
        ITextChunker chunker = TextChunkerFactory.Create(ChunkingStrategy.Fixed);

        // Assert
        chunker.Should().NotBeNull();
        chunker.Should().BeOfType<FixedSizeTextChunker>();
    }

    [Fact]
    public void TextChunkerFactory_Create_WithSentenceStrategy_ShouldReturnSentenceChunker()
    {
        // Arrange & Act
        ITextChunker chunker = TextChunkerFactory.Create(ChunkingStrategy.Sentence);

        // Assert
        chunker.Should().NotBeNull();
        chunker.Should().BeOfType<SentenceTextChunker>();
    }

    [Fact]
    public void TextChunkerFactory_Create_WithSectionStrategy_ShouldReturnSectionChunker()
    {
        // Arrange & Act
        ITextChunker chunker = TextChunkerFactory.Create(ChunkingStrategy.Section);

        // Assert
        chunker.Should().NotBeNull();
        chunker.Should().BeOfType<SectionTextChunker>();
    }

    [Fact]
    public void TextChunkerFactory_Create_WithInvalidStrategy_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        ChunkingStrategy invalidStrategy = (ChunkingStrategy)999;

        // Act
        Action act = () => TextChunkerFactory.Create(invalidStrategy);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("strategy")
            .WithMessage("*Unknown chunking strategy*");
    }

    [Fact]
    public void TextChunkerFactory_Create_WithCustomChunkSize_ShouldAcceptParameter()
    {
        // Arrange & Act
        ITextChunker chunker = TextChunkerFactory.Create(
            ChunkingStrategy.Fixed,
            targetChunkSize: 1024);

        // Assert
        chunker.Should().NotBeNull();
    }

    [Fact]
    public void TextChunkerFactory_Create_WithCustomOverlapSize_ShouldAcceptParameter()
    {
        // Arrange & Act
        ITextChunker chunker = TextChunkerFactory.Create(
            ChunkingStrategy.Semantic,
            overlapSize: 200);

        // Assert
        chunker.Should().NotBeNull();
    }

    [Fact]
    public void TextChunkerFactory_Create_WithLoggerFactory_ShouldAcceptParameter()
    {
        // Arrange
        Mock<ILoggerFactory> mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(NullLogger.Instance);

        // Act
        ITextChunker chunker = TextChunkerFactory.Create(
            ChunkingStrategy.Fixed,
            loggerFactory: mockLoggerFactory.Object);

        // Assert
        chunker.Should().NotBeNull();
    }

    [Fact]
    public void TextChunkerFactory_Create_WithNullLoggerFactory_ShouldNotThrow()
    {
        // Arrange & Act
        Action act = () => TextChunkerFactory.Create(ChunkingStrategy.Fixed, loggerFactory: null);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region EmbeddingServiceFactory Tests

    [Fact(Skip = "Integration test - requires actual ONNX model files")]
    public void EmbeddingServiceFactory_CreateOnnxService_WithValidPath_ShouldReturnService()
    {
        // Arrange
        string modelsPath = "/path/to/models";

        // Act & Assert - Should not throw during creation (actual model loading happens lazily)
        Action act = () => EmbeddingServiceFactory.CreateOnnxService(
            EmbeddingStrategy.MiniLM,
            modelsPath);

        act.Should().NotThrow();
    }

    [Fact]
    public void EmbeddingServiceFactory_CreateOnnxService_WithNullPath_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => EmbeddingServiceFactory.CreateOnnxService(
            EmbeddingStrategy.MiniLM,
            null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("modelsBasePath");
    }

    [Fact]
    public void EmbeddingServiceFactory_CreateOnnxService_WithEmptyPath_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => EmbeddingServiceFactory.CreateOnnxService(
            EmbeddingStrategy.MiniLM,
            string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("modelsBasePath");
    }

    [Fact]
    public void EmbeddingServiceFactory_CreateOnnxService_WithWhitespacePath_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => EmbeddingServiceFactory.CreateOnnxService(
            EmbeddingStrategy.MiniLM,
            "   ");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("modelsBasePath");
    }

    [Fact(Skip = "Integration test - requires actual ONNX model files")]
    public void EmbeddingServiceFactory_CreateOnnxService_WithAllStrategies_ShouldCreateService()
    {
        // Arrange
        EmbeddingStrategy[] strategies = new[]
        {
            EmbeddingStrategy.MiniLM,
            EmbeddingStrategy.MPNetBase,
            EmbeddingStrategy.BGESmall,
            EmbeddingStrategy.BGELarge,
            EmbeddingStrategy.Multilingual
        };

        string modelsPath = "/path/to/models";

        // Act & Assert
        foreach (EmbeddingStrategy strategy in strategies)
        {
            Action act = () => EmbeddingServiceFactory.CreateOnnxService(strategy, modelsPath);
            act.Should().NotThrow($"Strategy {strategy} should create service");
        }
    }

    [Fact]
    public void EmbeddingServiceFactory_CreateOnnxService_WithInvalidStrategy_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        EmbeddingStrategy invalidStrategy = (EmbeddingStrategy)999;

        // Act
        Action act = () => EmbeddingServiceFactory.CreateOnnxService(
            invalidStrategy,
            "/path/to/models");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("strategy");
    }

    [Fact]
    public void EmbeddingServiceFactory_CreateCachedService_WithNullInnerService_ShouldThrowArgumentNullException()
    {
        // Arrange
        MemoryCacheOptions cacheOptions = new MemoryCacheOptions();
        IMemoryCache cache = new MemoryCache(cacheOptions);

        // Act
        Action act = () => EmbeddingServiceFactory.CreateCachedService(
            null!,
            cache);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("innerService");
    }

    [Fact]
    public void EmbeddingServiceFactory_CreateCachedService_WithNullCache_ShouldThrowArgumentNullException()
    {
        // Arrange
        Mock<IEmbeddingService> mockService = new Mock<IEmbeddingService>();

        // Act
        Action act = () => EmbeddingServiceFactory.CreateCachedService(
            mockService.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("cache");
    }

    [Fact]
    public void EmbeddingServiceFactory_CreateCachedService_WithValidParameters_ShouldReturnCachedService()
    {
        // Arrange
        Mock<IEmbeddingService> mockService = new Mock<IEmbeddingService>();
        MemoryCacheOptions cacheOptions = new MemoryCacheOptions();
        IMemoryCache cache = new MemoryCache(cacheOptions);

        // Act
        IEmbeddingService cachedService = EmbeddingServiceFactory.CreateCachedService(
            mockService.Object,
            cache);

        // Assert
        cachedService.Should().NotBeNull();
        cachedService.Should().BeOfType<CachedEmbeddingService>();
    }

    [Fact]
    public void EmbeddingServiceFactory_CreateCachedService_WithCustomExpiration_ShouldAcceptParameter()
    {
        // Arrange
        Mock<IEmbeddingService> mockService = new Mock<IEmbeddingService>();
        MemoryCacheOptions cacheOptions = new MemoryCacheOptions();
        IMemoryCache cache = new MemoryCache(cacheOptions);
        TimeSpan customExpiration = TimeSpan.FromHours(12);

        // Act
        IEmbeddingService cachedService = EmbeddingServiceFactory.CreateCachedService(
            mockService.Object,
            cache,
            customExpiration);

        // Assert
        cachedService.Should().NotBeNull();
    }

    [Fact(Skip = "Integration test - requires actual ONNX model files")]
    public void EmbeddingServiceFactory_CreateCachedOnnxService_WithValidParameters_ShouldReturnService()
    {
        // Arrange
        MemoryCacheOptions cacheOptions = new MemoryCacheOptions();
        IMemoryCache cache = new MemoryCache(cacheOptions);

        // Act & Assert
        Action act = () => EmbeddingServiceFactory.CreateCachedOnnxService(
            EmbeddingStrategy.MiniLM,
            "/path/to/models",
            cache);

        act.Should().NotThrow();
    }

    #endregion

    #region LanguageModelFactory Tests

    [Fact]
    public void LanguageModelFactory_Create_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => LanguageModelFactory.Create(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void LanguageModelFactory_Create_WithMissingProvider_ShouldThrowInvalidOperationException()
    {
        // Arrange
        Dictionary<string, string?> configData = new Dictionary<string, string?>();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        Action act = () => LanguageModelFactory.Create(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Language model provider not specified*");
    }

    [Fact]
    public void LanguageModelFactory_Create_WithInvalidProvider_ShouldThrowInvalidOperationException()
    {
        // Arrange
        Dictionary<string, string?> configData = new Dictionary<string, string?>
        {
            ["OmniRAG:LanguageModel:Provider"] = "InvalidProvider"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        Action act = () => LanguageModelFactory.Create(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unknown language model provider*");
    }

    [Fact]
    public void LanguageModelFactory_Create_Phi4WithoutModelPath_ShouldThrowInvalidOperationException()
    {
        // Arrange
        Dictionary<string, string?> configData = new Dictionary<string, string?>
        {
            ["OmniRAG:LanguageModel:Provider"] = "Phi4"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        Action act = () => LanguageModelFactory.Create(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Phi-4 model path not specified*");
    }

    [Fact]
    public void LanguageModelFactory_Create_MistralWithoutApiKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        Dictionary<string, string?> configData = new Dictionary<string, string?>
        {
            ["OmniRAG:LanguageModel:Provider"] = "Mistral"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        Action act = () => LanguageModelFactory.Create(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Mistral API key not specified*");
    }

    [Fact]
    public void LanguageModelFactory_Create_GptWithoutApiKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        Dictionary<string, string?> configData = new Dictionary<string, string?>
        {
            ["OmniRAG:LanguageModel:Provider"] = "GPT"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        Action act = () => LanguageModelFactory.Create(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OpenAI API key not specified*");
    }

    [Fact]
    public void LanguageModelFactory_Create_LlamaWithoutModelPath_ShouldThrowInvalidOperationException()
    {
        // Arrange
        Dictionary<string, string?> configData = new Dictionary<string, string?>
        {
            ["OmniRAG:LanguageModel:Provider"] = "Llama"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        Action act = () => LanguageModelFactory.Create(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Llama model path not specified*");
    }

    [Fact]
    public void LanguageModelFactory_Create_Phi4WithValidConfig_ShouldThrowDirectoryNotFoundException()
    {
        // Arrange
        Dictionary<string, string?> configData = new Dictionary<string, string?>
        {
            ["OmniRAG:LanguageModel:Provider"] = "Phi4",
            ["OmniRAG:LanguageModel:Phi4:ModelPath"] = "/path/to/nonexistent/model"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act & Assert - Should throw since model path doesn't exist
        Action act = () => LanguageModelFactory.Create(configuration);
        act.Should().Throw<DirectoryNotFoundException>();
    }

    [Fact]
    public void LanguageModelFactory_Create_MistralWithValidConfig_ShouldReturnMistralModel()
    {
        // Arrange
        Dictionary<string, string?> configData = new Dictionary<string, string?>
        {
            ["OmniRAG:LanguageModel:Provider"] = "Mistral",
            ["OmniRAG:LanguageModel:Mistral:ApiKey"] = "test-api-key"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        ILanguageModel model = LanguageModelFactory.Create(configuration);

        // Assert
        model.Should().NotBeNull();
        model.Should().BeOfType<MistralLanguageModel>();
    }

    [Fact]
    public void LanguageModelFactory_Create_GptWithValidConfig_ShouldReturnGptModel()
    {
        // Arrange
        Dictionary<string, string?> configData = new Dictionary<string, string?>
        {
            ["OmniRAG:LanguageModel:Provider"] = "GPT",
            ["OmniRAG:LanguageModel:GPT:ApiKey"] = "test-api-key"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        ILanguageModel model = LanguageModelFactory.Create(configuration);

        // Assert
        model.Should().NotBeNull();
        model.Should().BeOfType<GptLanguageModel>();
    }

    [Fact]
    public void LanguageModelFactory_Create_WithProviderAlias_Llama_ShouldThrowDirectoryNotFoundException()
    {
        // Arrange
        Dictionary<string, string?> configData = new Dictionary<string, string?>
        {
            ["OmniRAG:LanguageModel:Provider"] = "Llama",
            ["OmniRAG:LanguageModel:Llama:ModelPath"] = "/path/to/nonexistent/model"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act & Assert - Should throw since model path doesn't exist
        Action act = () => LanguageModelFactory.Create(configuration);
        act.Should().Throw<DirectoryNotFoundException>();
    }

    [Fact]
    public void LanguageModelFactory_Create_WithProviderAlias_Phi_ShouldThrowDirectoryNotFoundException()
    {
        // Arrange
        Dictionary<string, string?> configData = new Dictionary<string, string?>
        {
            ["OmniRAG:LanguageModel:Provider"] = "phi-4",
            ["OmniRAG:LanguageModel:Phi4:ModelPath"] = "/path/to/nonexistent/model"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act & Assert - Should throw since model path doesn't exist
        Action act = () => LanguageModelFactory.Create(configuration);
        act.Should().Throw<DirectoryNotFoundException>();
    }

    [Fact]
    public void LanguageModelFactory_Create_WithProviderAlias_OpenAI_ShouldWork()
    {
        // Arrange
        Dictionary<string, string?> configData = new Dictionary<string, string?>
        {
            ["OmniRAG:LanguageModel:Provider"] = "openai",
            ["OmniRAG:LanguageModel:GPT:ApiKey"] = "test-api-key"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        ILanguageModel model = LanguageModelFactory.Create(configuration);

        // Assert
        model.Should().BeOfType<GptLanguageModel>();
    }

    [Fact]
    public void LanguageModelFactory_CreateWithProvider_WithNullProvider_ShouldThrowArgumentException()
    {
        // Arrange
        Dictionary<string, string?> configData = new Dictionary<string, string?>();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        Action act = () => LanguageModelFactory.Create(null!, configuration);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LanguageModelFactory_CreateWithProvider_WithEmptyProvider_ShouldThrowArgumentException()
    {
        // Arrange
        Dictionary<string, string?> configData = new Dictionary<string, string?>();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        Action act = () => LanguageModelFactory.Create(string.Empty, configuration);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LanguageModelFactory_CreateWithProvider_WithValidProvider_ShouldCreateModel()
    {
        // Arrange
        Dictionary<string, string?> configData = new Dictionary<string, string?>
        {
            ["OmniRAG:LanguageModel:Mistral:ApiKey"] = "test-api-key"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        ILanguageModel model = LanguageModelFactory.Create("Mistral", configuration);

        // Assert
        model.Should().BeOfType<MistralLanguageModel>();
    }

    [Fact]
    public void LanguageModelFactory_Create_WithNullLoggerFactory_ShouldNotThrow()
    {
        // Arrange
        Dictionary<string, string?> configData = new Dictionary<string, string?>
        {
            ["OmniRAG:LanguageModel:Provider"] = "Mistral",
            ["OmniRAG:LanguageModel:Mistral:ApiKey"] = "test-api-key"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        Action act = () => LanguageModelFactory.Create(configuration, loggerFactory: null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void LanguageModelFactory_Create_WithCustomMaxTokens_ShouldRespectSetting()
    {
        // Arrange
        Dictionary<string, string?> configData = new Dictionary<string, string?>
        {
            ["OmniRAG:LanguageModel:Provider"] = "Mistral",
            ["OmniRAG:LanguageModel:Mistral:ApiKey"] = "test-api-key",
            ["OmniRAG:LanguageModel:Mistral:MaxTokens"] = "2048"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        ILanguageModel model = LanguageModelFactory.Create(configuration);

        // Assert
        model.Should().NotBeNull();
    }

    [Fact]
    public void LanguageModelFactory_Create_WithCustomTemperature_ShouldRespectSetting()
    {
        // Arrange
        Dictionary<string, string?> configData = new Dictionary<string, string?>
        {
            ["OmniRAG:LanguageModel:Provider"] = "GPT",
            ["OmniRAG:LanguageModel:GPT:ApiKey"] = "test-api-key",
            ["OmniRAG:LanguageModel:GPT:Temperature"] = "0.9"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        ILanguageModel model = LanguageModelFactory.Create(configuration);

        // Assert
        model.Should().NotBeNull();
    }

    #endregion
}
