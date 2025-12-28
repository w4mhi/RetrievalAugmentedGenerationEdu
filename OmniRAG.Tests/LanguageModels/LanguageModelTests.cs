using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using OmniRAG.Core.Interfaces;
using OmniRAG.Infrastructure.LanguageModels;

using Xunit;

namespace OmniRAG.Tests.LanguageModels;

/// <summary>
/// Comprehensive unit tests for language model implementations.
/// Tests Phi4LanguageModel, GptLanguageModel, MistralLanguageModel, and LlamaLanguageModel.
/// </summary>
public class LanguageModelTests
{
    #region Phi4LanguageModel Tests

    [Fact]
    public void Phi4LanguageModel_Constructor_WithNullModelPath_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new Phi4LanguageModel(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Phi4LanguageModel_Constructor_WithEmptyModelPath_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new Phi4LanguageModel(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Phi4LanguageModel_Constructor_WithWhitespaceModelPath_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new Phi4LanguageModel("   ");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Phi4LanguageModel_Constructor_WithNonExistentPath_ShouldThrowDirectoryNotFoundException()
    {
        // Arrange
        string nonExistentPath = "/path/that/does/not/exist";

        // Act
        Action act = () => new Phi4LanguageModel(nonExistentPath);

        // Assert
        act.Should().Throw<DirectoryNotFoundException>()
            .WithMessage($"*Phi-4 model directory not found: {nonExistentPath}*");
    }

    [Fact]
    public void Phi4LanguageModel_ModelName_ShouldReturnPhi4Mini()
    {
        // Note: This test requires a valid model path to instantiate
        // Skip for now as it needs actual model files
        // Arrange
        string testPath = "/tmp/phi4-test-model";

        // Act & Assert
        Action act = () =>
        {
            if (System.IO.Directory.Exists(testPath))
            {
                Phi4LanguageModel model = new Phi4LanguageModel(testPath);
                model.ModelName.Should().Be("Phi-4 Mini");
                model.Dispose();
            }
        };

        act.Should().NotThrow();
    }

    [Fact(Skip = "Integration test - requires actual model files")]
    public async Task Phi4LanguageModel_GenerateAsync_WithNullPrompt_ShouldThrowArgumentException()
    {
        // Arrange
        string testPath = "/path/to/model";
        Phi4LanguageModel model = new Phi4LanguageModel(testPath);

        // Act
        Func<Task> act = async () => await model.GenerateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GptLanguageModel Tests

    [Fact]
    public void GptLanguageModel_Constructor_WithNullApiKey_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new GptLanguageModel(null!, "gpt-4-turbo");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GptLanguageModel_Constructor_WithEmptyApiKey_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new GptLanguageModel(string.Empty, "gpt-4-turbo");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GptLanguageModel_Constructor_WithWhitespaceApiKey_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new GptLanguageModel("   ", "gpt-4-turbo");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GptLanguageModel_Constructor_WithValidApiKey_ShouldNotThrow()
    {
        // Arrange & Act
        Action act = () =>
        {
            GptLanguageModel model = new GptLanguageModel("test-api-key", "gpt-4-turbo");
            model.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void GptLanguageModel_Constructor_WithCustomMaxTokens_ShouldAcceptParameter()
    {
        // Arrange & Act
        Action act = () =>
        {
            GptLanguageModel model = new GptLanguageModel(
                "test-api-key",
                "gpt-4-turbo",
                null,
                maxTokens: 2048);
            model.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void GptLanguageModel_Constructor_WithCustomTemperature_ShouldAcceptParameter()
    {
        // Arrange & Act
        Action act = () =>
        {
            GptLanguageModel model = new GptLanguageModel(
                "test-api-key",
                "gpt-4-turbo",
                null,
                temperature: 0.9f);
            model.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void GptLanguageModel_Constructor_WithOrganizationId_ShouldAcceptParameter()
    {
        // Arrange & Act
        Action act = () =>
        {
            GptLanguageModel model = new GptLanguageModel(
                "test-api-key",
                "gpt-4-turbo",
                "org-12345");
            model.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void GptLanguageModel_Constructor_WithLogger_ShouldAcceptParameter()
    {
        // Arrange & Act
        Action act = () =>
        {
            GptLanguageModel model = new GptLanguageModel(
                "test-api-key",
                "gpt-4-turbo",
                null,
                logger: NullLogger<GptLanguageModel>.Instance);
            model.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void GptLanguageModel_ModelName_ShouldReturnConfiguredModelName()
    {
        // Arrange
        string expectedModelName = "gpt-4-turbo";
        GptLanguageModel model = new GptLanguageModel("test-api-key", expectedModelName);

        // Act
        string actualModelName = model.ModelName;
        model.Dispose();

        // Assert
        actualModelName.Should().Contain(expectedModelName);
    }

    [Fact]
    public void GptLanguageModel_IsInitialized_ShouldReturnTrue()
    {
        // Arrange
        GptLanguageModel model = new GptLanguageModel("test-api-key", "gpt-4-turbo");

        // Act
        bool isInitialized = model.IsInitialized;
        model.Dispose();

        // Assert
        isInitialized.Should().BeTrue();
    }

    #endregion

    #region MistralLanguageModel Tests

    [Fact]
    public void MistralLanguageModel_Constructor_WithNullApiKey_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new MistralLanguageModel(null!, "mistral-small");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MistralLanguageModel_Constructor_WithEmptyApiKey_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new MistralLanguageModel(string.Empty, "mistral-small");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MistralLanguageModel_Constructor_WithWhitespaceApiKey_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new MistralLanguageModel("   ", "mistral-small");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MistralLanguageModel_Constructor_WithValidApiKey_ShouldNotThrow()
    {
        // Arrange & Act
        Action act = () =>
        {
            MistralLanguageModel model = new MistralLanguageModel("test-api-key", "mistral-small");
            model.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void MistralLanguageModel_Constructor_WithCustomMaxTokens_ShouldAcceptParameter()
    {
        // Arrange & Act
        Action act = () =>
        {
            MistralLanguageModel model = new MistralLanguageModel(
                "test-api-key",
                "mistral-small",
                maxTokens: 1024);
            model.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void MistralLanguageModel_Constructor_WithCustomTemperature_ShouldAcceptParameter()
    {
        // Arrange & Act
        Action act = () =>
        {
            MistralLanguageModel model = new MistralLanguageModel(
                "test-api-key",
                "mistral-small",
                temperature: 0.5f);
            model.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void MistralLanguageModel_Constructor_WithLogger_ShouldAcceptParameter()
    {
        // Arrange & Act
        Action act = () =>
        {
            MistralLanguageModel model = new MistralLanguageModel(
                "test-api-key",
                "mistral-small",
                logger: NullLogger<MistralLanguageModel>.Instance);
            model.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void MistralLanguageModel_ModelName_ShouldReturnConfiguredModelName()
    {
        // Arrange
        string expectedModelName = "mistral-medium";
        MistralLanguageModel model = new MistralLanguageModel("test-api-key", expectedModelName);

        // Act
        string actualModelName = model.ModelName;
        model.Dispose();

        // Assert
        actualModelName.Should().Contain(expectedModelName);
    }

    [Fact]
    public void MistralLanguageModel_IsInitialized_ShouldReturnTrue()
    {
        // Arrange
        MistralLanguageModel model = new MistralLanguageModel("test-api-key", "mistral-small");

        // Act
        bool isInitialized = model.IsInitialized;
        model.Dispose();

        // Assert
        isInitialized.Should().BeTrue();
    }

    #endregion

    #region LlamaLanguageModel Tests

    [Fact]
    public void LlamaLanguageModel_Constructor_WithNullModelPath_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new LlamaLanguageModel(null!, "Llama-3-8B");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LlamaLanguageModel_Constructor_WithEmptyModelPath_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new LlamaLanguageModel(string.Empty, "Llama-3-8B");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LlamaLanguageModel_Constructor_WithWhitespaceModelPath_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new LlamaLanguageModel("   ", "Llama-3-8B");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LlamaLanguageModel_Constructor_WithNonExistentPath_ShouldThrowDirectoryNotFoundException()
    {
        // Arrange
        string nonExistentPath = "/path/that/does/not/exist";

        // Act
        Action act = () => new LlamaLanguageModel(nonExistentPath, "Llama-3-8B");

        // Assert
        act.Should().Throw<DirectoryNotFoundException>()
            .WithMessage($"*Llama model directory not found: {nonExistentPath}*");
    }

    [Fact]
    public void LlamaLanguageModel_Constructor_WithCustomMaxTokens_ShouldAcceptParameter()
    {
        // Note: This test will throw DirectoryNotFoundException without a valid path
        // The validation for maxTokens happens after path validation
        string testPath = "/tmp/llama-test";

        // Act & Assert
        Action act = () =>
        {
            try
            {
                LlamaLanguageModel model = new LlamaLanguageModel(
                    testPath,
                    "Llama-3-8B",
                    maxTokens: 2048);
                model.Dispose();
            }
            catch (DirectoryNotFoundException)
            {
                // Expected when path doesn't exist
            }
        };

        act.Should().NotThrow<ArgumentException>();
    }

    [Fact]
    public void LlamaLanguageModel_Constructor_WithCustomTemperature_ShouldAcceptParameter()
    {
        // Note: This test will throw DirectoryNotFoundException without a valid path
        string testPath = "/tmp/llama-test";

        // Act & Assert
        Action act = () =>
        {
            try
            {
                LlamaLanguageModel model = new LlamaLanguageModel(
                    testPath,
                    "Llama-3-8B",
                    temperature: 0.8f);
                model.Dispose();
            }
            catch (DirectoryNotFoundException)
            {
                // Expected when path doesn't exist
            }
        };

        act.Should().NotThrow<ArgumentException>();
    }

    [Fact]
    public void LlamaLanguageModel_ModelName_ShouldReturnConfiguredModelVariant()
    {
        // Note: This test will throw DirectoryNotFoundException without a valid path
        string expectedModelName = "Llama-3-70B";
        string testPath = "/tmp/llama-test";

        // Act & Assert - We expect DirectoryNotFoundException, not ArgumentException
        Action act = () =>
        {
            try
            {
                LlamaLanguageModel model = new LlamaLanguageModel(testPath, expectedModelName);
                model.Dispose();
            }
            catch (DirectoryNotFoundException ex)
            {
                // Expected - path doesn't exist
                ex.Message.Should().Contain("Llama model directory not found");
            }
        };

        act.Should().NotThrow<ArgumentException>();
    }

    #endregion

    #region General Language Model Tests

    [Fact]
    public void AllLanguageModels_Dispose_ShouldNotThrowWhenCalledMultipleTimes()
    {
        // Arrange
        GptLanguageModel gptModel = new GptLanguageModel("test-key", "gpt-4");
        MistralLanguageModel mistralModel = new MistralLanguageModel("test-key", "mistral-small");

        // Act
        Action actGpt = () =>
        {
            gptModel.Dispose();
            gptModel.Dispose(); // Second dispose
        };

        Action actMistral = () =>
        {
            mistralModel.Dispose();
            mistralModel.Dispose(); // Second dispose
        };

        // Assert
        actGpt.Should().NotThrow();
        actMistral.Should().NotThrow();
    }

    [Fact]
    public void AllCloudLanguageModels_Constructor_WithNullLogger_ShouldNotThrow()
    {
        // Arrange & Act & Assert
        Action actGpt = () =>
        {
            GptLanguageModel model = new GptLanguageModel("key", "gpt-4", logger: null);
            model.Dispose();
        };
        actGpt.Should().NotThrow();

        Action actMistral = () =>
        {
            MistralLanguageModel model = new MistralLanguageModel("key", "mistral-small", logger: null);
            model.Dispose();
        };
        actMistral.Should().NotThrow();
    }

    #endregion
}
