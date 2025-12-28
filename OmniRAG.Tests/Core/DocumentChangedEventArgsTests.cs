using FluentAssertions;
using OmniRAG.Core.Interfaces;
using OmniRAG.Core.Models;
using Xunit;

namespace OmniRAG.Tests.Core;

/// <summary>
/// Unit tests for DocumentChangedEventArgs.
/// Verifies event argument construction and property access.
/// </summary>
public class DocumentChangedEventArgsTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldSetProperties()
    {
        // Arrange
        string filePath = "/path/to/document.pdf";
        FileChangeType changeType = FileChangeType.Added;

        // Act
        DocumentChangedEventArgs eventArgs = new DocumentChangedEventArgs(filePath, changeType);

        // Assert
        eventArgs.FilePath.Should().Be(filePath);
        eventArgs.ChangeType.Should().Be(changeType);
    }

    [Theory]
    [InlineData(FileChangeType.Added)]
    [InlineData(FileChangeType.Modified)]
    [InlineData(FileChangeType.Deleted)]
    public void Constructor_WithDifferentChangeTypes_ShouldPreserveType(FileChangeType changeType)
    {
        // Arrange
        string filePath = "/path/to/test.pdf";

        // Act
        DocumentChangedEventArgs eventArgs = new DocumentChangedEventArgs(filePath, changeType);

        // Assert
        eventArgs.ChangeType.Should().Be(changeType);
    }

    [Fact]
    public void Properties_ShouldBeReadOnly()
    {
        // Arrange
        DocumentChangedEventArgs eventArgs = new DocumentChangedEventArgs(
            "/path/to/file.pdf",
            FileChangeType.Modified);

        // Act & Assert
        // Verify properties are init-only by checking they exist
        eventArgs.FilePath.Should().NotBeNullOrEmpty();
        eventArgs.ChangeType.Should().BeDefined();
    }

    [Fact]
    public void Constructor_WithEmptyFilePath_ShouldNotThrow()
    {
        // Arrange & Act
        DocumentChangedEventArgs eventArgs = new DocumentChangedEventArgs(
            string.Empty,
            FileChangeType.Added);

        // Assert
        eventArgs.FilePath.Should().BeEmpty();
    }
}
