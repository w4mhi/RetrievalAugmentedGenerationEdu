using System;
using System.Collections.Generic;

using FluentAssertions;

using OmniRAG.Core.Exceptions;

using Xunit;

namespace OmniRAG.Tests.Core;

/// <summary>
/// Unit tests for ConfigurationValidationException.
/// Ensures proper exception construction and message handling.
/// </summary>
public class ConfigurationValidationExceptionTests
{
    [Fact]
    public void Constructor_WithSingleError_ShouldFormatMessage()
    {
        // Arrange
        string[] errors = new[] { "Python DLL path is required" };

        // Act
        ConfigurationValidationException exception = new ConfigurationValidationException(errors);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Configuration validation failed");
        exception.ToString().Should().Contain("Python DLL path is required");
        exception.ValidationErrors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void Constructor_WithMultipleErrors_ShouldIncludeAllErrors()
    {
        // Arrange
        string[] errors = new[]
        {
            "Python DLL path is required",
            "ChromaDB directory is invalid",
            "No language model is enabled"
        };

        // Act
        ConfigurationValidationException exception = new ConfigurationValidationException(errors);

        // Assert
        exception.ValidationErrors.Should().HaveCount(3);
        exception.ValidationErrors.Should().BeEquivalentTo(errors);
        string exceptionString = exception.ToString();
        exceptionString.Should().Contain("Python DLL path is required");
        exceptionString.Should().Contain("ChromaDB directory is invalid");
        exceptionString.Should().Contain("No language model is enabled");
    }

    [Fact]
    public void Constructor_WithEmptyErrorsArray_ShouldNotThrow()
    {
        // Arrange
        string[] errors = Array.Empty<string>();

        // Act
        ConfigurationValidationException exception = new ConfigurationValidationException(errors);

        // Assert
        exception.Should().NotBeNull();
        exception.ValidationErrors.Should().BeEmpty();
        exception.Message.Should().Contain("Configuration validation failed");
    }

    [Fact]
    public void ValidationErrors_ShouldBeReadOnly()
    {
        // Arrange
        string[] errors = new[] { "Error 1", "Error 2" };
        ConfigurationValidationException exception = new ConfigurationValidationException(errors);

        // Act & Assert
        exception.ValidationErrors.Should().BeAssignableTo<IReadOnlyList<string>>();
    }
}
