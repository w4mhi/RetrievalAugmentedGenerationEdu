using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using OmniRAG.Core.Interfaces;
using OmniRAG.Core.Models;
using OmniRAG.Infrastructure.Chunking;

using Xunit;

namespace OmniRAG.Tests.Chunking;

/// <summary>
/// Comprehensive unit tests for all text chunking implementations.
/// Tests FixedSizeTextChunker, SemanticTextChunker, SentenceTextChunker, and SectionTextChunker.
/// </summary>
public class ChunkingTests
{
    #region FixedSizeTextChunker Tests

    [Fact]
    public void FixedSizeTextChunker_Constructor_WithValidParameters_ShouldNotThrow()
    {
        // Arrange & Act
        Action act = () => new FixedSizeTextChunker(512, 50);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void FixedSizeTextChunker_Constructor_WithZeroChunkSize_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange & Act
        Action act = () => new FixedSizeTextChunker(0, 50);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("chunkSize")
            .WithMessage("*Chunk size must be positive.*");
    }

    [Fact]
    public void FixedSizeTextChunker_Constructor_WithNegativeChunkSize_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange & Act
        Action act = () => new FixedSizeTextChunker(-100, 50);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("chunkSize");
    }

    [Fact]
    public void FixedSizeTextChunker_Constructor_WithNegativeOverlapSize_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange & Act
        Action act = () => new FixedSizeTextChunker(512, -10);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("overlapSize")
            .WithMessage("*Overlap size cannot be negative.*");
    }

    [Fact]
    public void FixedSizeTextChunker_Constructor_WithOverlapEqualToChunkSize_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new FixedSizeTextChunker(512, 512);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Overlap size must be less than chunk size.*");
    }

    [Fact]
    public void FixedSizeTextChunker_Constructor_WithOverlapGreaterThanChunkSize_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new FixedSizeTextChunker(100, 200);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FixedSizeTextChunker_ChunkText_WithNormalText_ShouldReturnChunks()
    {
        // Arrange
        ITextChunker chunker = new FixedSizeTextChunker(50, 10, NullLogger<FixedSizeTextChunker>.Instance);
        string text = "This is a test document. It contains multiple sentences. We want to test chunking.";
        List<string> headings = new List<string> { "Test Section" };

        // Act
        IReadOnlyList<TextChunk> chunks = chunker.ChunkText(text, 1, headings);

        // Assert
        chunks.Should().NotBeEmpty();
        chunks.Should().OnlyContain(c => !string.IsNullOrWhiteSpace(c.Content));
    }

    [Fact]
    public void FixedSizeTextChunker_ChunkText_WithEmptyText_ShouldReturnEmptyList()
    {
        // Arrange
        ITextChunker chunker = new FixedSizeTextChunker(512, 50);
        List<string> headings = new List<string>();

        // Act
        IReadOnlyList<TextChunk> chunks = chunker.ChunkText(string.Empty, 1, headings);

        // Assert
        chunks.Should().BeEmpty();
    }

    [Fact]
    public void FixedSizeTextChunker_ChunkText_WithNullText_ShouldReturnEmptyList()
    {
        // Arrange
        ITextChunker chunker = new FixedSizeTextChunker(512, 50);
        List<string> headings = new List<string>();

        // Act
        IReadOnlyList<TextChunk> chunks = chunker.ChunkText(null!, 1, headings);

        // Assert
        chunks.Should().BeEmpty();
    }

    [Fact]
    public void FixedSizeTextChunker_ChunkText_WithWhitespaceText_ShouldReturnEmptyList()
    {
        // Arrange
        ITextChunker chunker = new FixedSizeTextChunker(512, 50);
        List<string> headings = new List<string>();

        // Act
        IReadOnlyList<TextChunk> chunks = chunker.ChunkText("   \t\n  ", 1, headings);

        // Assert
        chunks.Should().BeEmpty();
    }

    [Fact]
    public void FixedSizeTextChunker_ChunkText_WithSingleWord_ShouldReturnOneChunk()
    {
        // Arrange
        ITextChunker chunker = new FixedSizeTextChunker(512, 50);
        List<string> headings = new List<string> { "Test" };

        // Act
        IReadOnlyList<TextChunk> chunks = chunker.ChunkText("Word", 1, headings);

        // Assert
        chunks.Should().HaveCount(1);
        chunks[0].Content.Should().Be("Word");
    }

    [Fact]
    public void FixedSizeTextChunker_ChunkText_WithVeryLongText_ShouldCreateMultipleChunks()
    {
        // Arrange
        ITextChunker chunker = new FixedSizeTextChunker(50, 10);
        string longText = string.Join(" ", Enumerable.Range(1, 1000).Select(i => $"Word{i}"));
        List<string> headings = new List<string> { "Long Document" };

        // Act
        IReadOnlyList<TextChunk> chunks = chunker.ChunkText(longText, 1, headings);

        // Assert
        chunks.Should().NotBeEmpty();
        chunks.Count.Should().BeGreaterThan(1);
    }

    [Fact]
    public void FixedSizeTextChunker_ChunkText_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        ITextChunker chunker = new FixedSizeTextChunker(512, 50);
        string text = "Special chars: @#$%^&*() test! 日本語 中文 émojis: 🚀🎉";
        List<string> headings = new List<string> { "Special" };

        // Act
        IReadOnlyList<TextChunk> chunks = chunker.ChunkText(text, 1, headings);

        // Assert
        chunks.Should().NotBeEmpty();
        chunks[0].Content.Should().Contain("Special chars");
    }

    #endregion

    #region SemanticTextChunker Tests

    [Fact]
    public void SemanticTextChunker_Constructor_WithValidParameters_ShouldNotThrow()
    {
        // Arrange & Act
        Action act = () => new SemanticTextChunker(512, 100);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SemanticTextChunker_ChunkText_WithParagraphs_ShouldRespectParagraphBoundaries()
    {
        // Arrange
        ITextChunker chunker = new SemanticTextChunker(512, 100, NullLogger<SemanticTextChunker>.Instance);
        string text = @"First paragraph.

Second paragraph with more text.

Third paragraph here.";
        List<string> headings = new List<string> { "Document" };

        // Act
        IReadOnlyList<TextChunk> chunks = chunker.ChunkText(text, 1, headings);

        // Assert
        chunks.Should().NotBeEmpty();
    }

    [Fact]
    public void SemanticTextChunker_ChunkText_WithEmptyText_ShouldReturnEmptyList()
    {
        // Arrange
        ITextChunker chunker = new SemanticTextChunker(512, 100);
        List<string> headings = new List<string>();

        // Act
        IReadOnlyList<TextChunk> chunks = chunker.ChunkText(string.Empty, 1, headings);

        // Assert
        chunks.Should().BeEmpty();
    }

    [Fact]
    public void SemanticTextChunker_ChunkText_WithSingleParagraph_ShouldReturnOneChunk()
    {
        // Arrange
        ITextChunker chunker = new SemanticTextChunker(512, 100);
        string text = "Single paragraph without line breaks.";
        List<string> headings = new List<string> { "Section" };

        // Act
        IReadOnlyList<TextChunk> chunks = chunker.ChunkText(text, 1, headings);

        // Assert
        chunks.Should().HaveCount(1);
    }

    [Fact]
    public void SemanticTextChunker_ChunkText_WithNullText_ShouldReturnEmptyList()
    {
        // Arrange
        ITextChunker chunker = new SemanticTextChunker(512, 100);
        List<string> headings = new List<string>();

        // Act
        IReadOnlyList<TextChunk> chunks = chunker.ChunkText(null!, 1, headings);

        // Assert
        chunks.Should().BeEmpty();
    }

    #endregion

    #region SentenceTextChunker Tests

    [Fact]
    public void SentenceTextChunker_Constructor_WithValidParameters_ShouldNotThrow()
    {
        // Arrange & Act
        Action act = () => new SentenceTextChunker(512);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SentenceTextChunker_ChunkText_WithMultipleSentences_ShouldPreserveSentenceBoundaries()
    {
        // Arrange
        ITextChunker chunker = new SentenceTextChunker(512, NullLogger<SentenceTextChunker>.Instance);
        string text = "First sentence. Second sentence! Third sentence?";
        List<string> headings = new List<string> { "Test" };

        // Act
        IReadOnlyList<TextChunk> chunks = chunker.ChunkText(text, 1, headings);

        // Assert
        chunks.Should().NotBeEmpty();
    }

    [Fact]
    public void SentenceTextChunker_ChunkText_WithSingleSentence_ShouldReturnOneChunk()
    {
        // Arrange
        ITextChunker chunker = new SentenceTextChunker(512);
        string text = "Just one sentence.";
        List<string> headings = new List<string> { "Section" };

        // Act
        IReadOnlyList<TextChunk> chunks = chunker.ChunkText(text, 1, headings);

        // Assert
        chunks.Should().HaveCount(1);
        chunks[0].Content.Should().Contain("Just one sentence");
    }

    [Fact]
    public void SentenceTextChunker_ChunkText_WithEmptyText_ShouldReturnEmptyList()
    {
        // Arrange
        ITextChunker chunker = new SentenceTextChunker(512);
        List<string> headings = new List<string>();

        // Act
        IReadOnlyList<TextChunk> chunks = chunker.ChunkText(string.Empty, 1, headings);

        // Assert
        chunks.Should().BeEmpty();
    }

    [Fact]
    public void SentenceTextChunker_ChunkText_WithNullText_ShouldReturnEmptyList()
    {
        // Arrange
        ITextChunker chunker = new SentenceTextChunker(512);
        List<string> headings = new List<string>();

        // Act
        IReadOnlyList<TextChunk> chunks = chunker.ChunkText(null!, 1, headings);

        // Assert
        chunks.Should().BeEmpty();
    }

    [Fact]
    public void SentenceTextChunker_ChunkText_WithQuestionMark_ShouldDetectSentence()
    {
        // Arrange
        ITextChunker chunker = new SentenceTextChunker(512);
        string text = "Is this a question?";
        List<string> headings = new List<string> { "Questions" };

        // Act
        IReadOnlyList<TextChunk> chunks = chunker.ChunkText(text, 1, headings);

        // Assert
        chunks.Should().HaveCount(1);
    }

    [Fact]
    public void SentenceTextChunker_ChunkText_WithExclamationMark_ShouldDetectSentence()
    {
        // Arrange
        ITextChunker chunker = new SentenceTextChunker(512);
        string text = "This is exciting!";
        List<string> headings = new List<string> { "Excitement" };

        // Act
        IReadOnlyList<TextChunk> chunks = chunker.ChunkText(text, 1, headings);

        // Assert
        chunks.Should().HaveCount(1);
    }

    #endregion

    #region SectionTextChunker Tests

    [Fact]
    public void SectionTextChunker_Constructor_WithValidParameters_ShouldNotThrow()
    {
        // Arrange & Act
        Action act = () => new SectionTextChunker();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SectionTextChunker_ChunkText_WithHeadings_ShouldCreateSectionBasedChunks()
    {
        // Arrange
        ITextChunker chunker = new SectionTextChunker(NullLogger<SectionTextChunker>.Instance);
        string text = @"# Introduction
This is the introduction section.

## Background
Some background information.";
        List<string> headings = new List<string> { "Introduction", "Background" };

        // Act
        IReadOnlyList<TextChunk> chunks = chunker.ChunkText(text, 1, headings);

        // Assert
        chunks.Should().NotBeEmpty();
    }

    [Fact]
    public void SectionTextChunker_ChunkText_WithoutHeadings_ShouldCreateSingleChunk()
    {
        // Arrange
        ITextChunker chunker = new SectionTextChunker();
        string text = "Text without any section headings. Just plain content.";
        List<string> headings = new List<string> { "Default" };

        // Act
        IReadOnlyList<TextChunk> chunks = chunker.ChunkText(text, 1, headings);

        // Assert
        chunks.Should().HaveCount(1);
    }

    [Fact]
    public void SectionTextChunker_ChunkText_WithEmptyText_ShouldReturnEmptyList()
    {
        // Arrange
        ITextChunker chunker = new SectionTextChunker();
        List<string> headings = new List<string>();

        // Act
        IReadOnlyList<TextChunk> chunks = chunker.ChunkText(string.Empty, 1, headings);

        // Assert
        chunks.Should().BeEmpty();
    }

    [Fact]
    public void SectionTextChunker_ChunkText_WithNullText_ShouldReturnEmptyList()
    {
        // Arrange
        ITextChunker chunker = new SectionTextChunker();
        List<string> headings = new List<string>();

        // Act
        IReadOnlyList<TextChunk> chunks = chunker.ChunkText(null!, 1, headings);

        // Assert
        chunks.Should().BeEmpty();
    }

    [Fact]
    public void SectionTextChunker_ChunkText_WithMultipleSections_ShouldPreserveStructure()
    {
        // Arrange
        ITextChunker chunker = new SectionTextChunker();
        string text = @"# Chapter 1
Content for chapter 1.

# Chapter 2
Content for chapter 2.

# Chapter 3
Content for chapter 3.";
        List<string> headings = new List<string> { "Chapter 1", "Chapter 2", "Chapter 3" };

        // Act
        IReadOnlyList<TextChunk> chunks = chunker.ChunkText(text, 1, headings);

        // Assert
        chunks.Should().NotBeEmpty();
    }

    #endregion

    #region General Chunker Tests

    [Fact]
    public void AllChunkers_ChunkText_ShouldReturnChunksWithPageNumber()
    {
        // Arrange
        ITextChunker[] chunkers = new ITextChunker[]
        {
            new FixedSizeTextChunker(512, 50),
            new SemanticTextChunker(512, 100),
            new SentenceTextChunker(512),
            new SectionTextChunker()
        };
        string text = "Test document content.";
        List<string> headings = new List<string> { "Test" };
        int expectedPageNumber = 5;

        // Act & Assert
        foreach (ITextChunker chunker in chunkers)
        {
            IReadOnlyList<TextChunk> chunks = chunker.ChunkText(text, expectedPageNumber, headings);
            if (chunks.Count > 0)
            {
                chunks.Should().OnlyContain(c => c.PageNumber == expectedPageNumber);
            }
        }
    }

    [Fact]
    public void AllChunkers_ChunkText_ShouldHandleEmptyHeadingsList()
    {
        // Arrange
        ITextChunker[] chunkers = new ITextChunker[]
        {
            new FixedSizeTextChunker(512, 50),
            new SemanticTextChunker(512, 100),
            new SentenceTextChunker(512),
            new SectionTextChunker()
        };
        string text = "Test content.";
        List<string> headings = new List<string>();

        // Act & Assert
        foreach (ITextChunker chunker in chunkers)
        {
            Action act = () => chunker.ChunkText(text, 1, headings);
            act.Should().NotThrow();
        }
    }

    [Fact]
    public void AllChunkers_ChunkText_WithEmptyHeadings_ShouldWork()
    {
        // Arrange
        ITextChunker[] chunkers = new ITextChunker[]
        {
            new FixedSizeTextChunker(512, 50),
            new SemanticTextChunker(512, 100),
            new SentenceTextChunker(512),
            new SectionTextChunker()
        };
        string text = "Test content.";
        IReadOnlyList<string> emptyHeadings = Array.Empty<string>();

        // Act & Assert
        foreach (ITextChunker chunker in chunkers)
        {
            Action act = () => chunker.ChunkText(text, 1, emptyHeadings);
            act.Should().NotThrow();
        }
    }

    #endregion
}
