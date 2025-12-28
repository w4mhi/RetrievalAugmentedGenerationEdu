using System;
using System.Collections.Generic;

using FluentAssertions;

using OmniRAG.Core.Models;

namespace OmniRAG.Tests.Core;

/// <summary>
/// Unit tests for DocumentChunk model.
/// Following Kent Beck''s TDD principles.
/// </summary>
public class DocumentChunkTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateChunk()
    {
        // Arrange
        string content = "Test content";
        string sourceFile = "test.pdf";
        int pageNumber = 1;
        string sectionTitle = "Introduction";
        float[] embedding = new float[] { 0.1f, 0.2f, 0.3f };

        // Act
        DocumentChunk chunk = DocumentChunk.Create(content, sourceFile, pageNumber, sectionTitle, embedding);

        // Assert
        chunk.Should().NotBeNull();
        chunk.Id.Should().NotBeNullOrEmpty();
        chunk.Content.Should().Be(content);
        chunk.SourceFilePath.Should().Be(sourceFile);
        chunk.PageNumber.Should().Be(pageNumber);
        chunk.SectionTitle.Should().Be(sectionTitle);
        chunk.Embedding.Should().BeEquivalentTo(embedding);
        chunk.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithNullMetadata_ShouldCreateEmptyMetadata()
    {
        // Arrange & Act
        DocumentChunk chunk = DocumentChunk.Create(
            "content",
            "file.pdf",
            1,
            "section",
            new float[] { 0.1f },
            metadata: null);

        // Assert
        chunk.Metadata.Should().NotBeNull();
        chunk.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithCustomMetadata_ShouldPreserveMetadata()
    {
        // Arrange
        Dictionary<string, object> metadata = new Dictionary<string, object>
        {
            ["custom"] = "value",
            ["number"] = 42
        };

        // Act
        DocumentChunk chunk = DocumentChunk.Create(
            "content",
            "file.pdf",
            1,
            "section",
            new float[] { 0.1f },
            metadata);

        // Assert
        chunk.Metadata.Should().ContainKey("custom");
        chunk.Metadata["custom"].Should().Be("value");
        chunk.Metadata["number"].Should().Be(42);
    }
}
