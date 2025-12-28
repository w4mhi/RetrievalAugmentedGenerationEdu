using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OmniRAG.Core.Constants;
using OmniRAG.Core.Interfaces;
using OmniRAG.Core.Models;
using OmniRAG.Core.Services;
using System.Reflection;
using Xunit;

namespace OmniRAG.Tests.Services;

/// <summary>
/// Unit tests for RagEngine helper methods extracted during Rule 16 refactoring.
/// Tests BuildLlmPrompt and AppendSearchResultContext private methods via reflection.
/// </summary>
public class RagEngineHelperMethodsTests
{
    private readonly Mock<IDocumentLoader> mockDocumentLoader;
    private readonly Mock<IEmbeddingService> mockEmbeddingService;
    private readonly Mock<IVectorStore> mockVectorStore;
    private readonly Mock<ILanguageModel> mockLanguageModel;
    private readonly RagEngine ragEngine;

    public RagEngineHelperMethodsTests()
    {
        this.mockDocumentLoader = new Mock<IDocumentLoader>();
        this.mockEmbeddingService = new Mock<IEmbeddingService>();
        this.mockVectorStore = new Mock<IVectorStore>();
        this.mockLanguageModel = new Mock<ILanguageModel>();

        RetrievalOptions options = RetrievalOptions.Create(RetrievalStrategy.TopK, 5, 0.7f);

        this.ragEngine = new RagEngine(
            this.mockDocumentLoader.Object,
            this.mockEmbeddingService.Object,
            this.mockVectorStore.Object,
            this.mockLanguageModel.Object,
            options,
            null);
    }

    [Fact]
    public void BuildLlmPrompt_WithValidResults_ShouldIncludeQueryAndInstructions()
    {
        // Arrange
        string query = "What is the frequency range?";
        List<SearchResult> searchResults = new List<SearchResult>
        {
            CreateSearchResult("The frequency range is 136-174 MHz", 0.95f, 1),
            CreateSearchResult("Maximum power output is 5 watts", 0.85f, 2)
        };

        // Act
        string prompt = InvokeBuildLlmPrompt(query, searchResults);

        // Assert
        prompt.Should().Contain("User Question: What is the frequency range?");
        prompt.Should().Contain(SystemPrompts.AnswerInstructions);
        prompt.Should().Contain("136-174 MHz");
        prompt.Should().Contain("5 watts");
        prompt.Should().Contain("--- Source 1 ---");
        prompt.Should().Contain("--- Source 2 ---");
    }

    [Fact]
    public void BuildLlmPrompt_ShouldIncludeSourceMetadata()
    {
        // Arrange
        string query = "Test query";
        List<SearchResult> searchResults = new List<SearchResult>
        {
            CreateSearchResult("Content here", 0.9f, 1, "manual.pdf", 42, "Chapter 5")
        };

        // Act
        string prompt = InvokeBuildLlmPrompt(query, searchResults);

        // Assert
        prompt.Should().Contain("Document: manual.pdf");
        prompt.Should().Contain("Page: 42");
        prompt.Should().Contain("Section: Chapter 5");
        prompt.Should().Contain("Relevance Score: 90");
    }

    [Fact]
    public void BuildLlmPrompt_WithMultipleResults_ShouldNumberSourcesSequentially()
    {
        // Arrange
        string query = "test";
        List<SearchResult> searchResults = new List<SearchResult>
        {
            CreateSearchResult("First result", 0.95f, 1),
            CreateSearchResult("Second result", 0.85f, 2),
            CreateSearchResult("Third result", 0.75f, 3)
        };

        // Act
        string prompt = InvokeBuildLlmPrompt(query, searchResults);

        // Assert
        prompt.Should().Contain("--- Source 1 ---");
        prompt.Should().Contain("--- Source 2 ---");
        prompt.Should().Contain("--- Source 3 ---");
    }

    private SearchResult CreateSearchResult(
        string content,
        float score,
        int rank,
        string fileName = "test.pdf",
        int pageNumber = 1,
        string section = "Section 1")
    {
        DocumentChunk chunk = DocumentChunk.Create(
            content,
            fileName,
            pageNumber,
            section,
            new float[] { 0.1f, 0.2f, 0.3f });

        return SearchResult.Create(chunk, score, rank);
    }

    private string InvokeBuildLlmPrompt(string query, IReadOnlyList<SearchResult> searchResults)
    {
        MethodInfo? method = typeof(RagEngine).GetMethod(
            "BuildLlmPrompt",
            BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull("BuildLlmPrompt method should exist");

        object? result = method!.Invoke(null, new object[] { query, searchResults });

        return result as string ?? string.Empty;
    }
}
