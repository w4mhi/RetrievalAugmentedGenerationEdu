using Microsoft.Extensions.Logging;
using OmniRAG.Core.Interfaces;
using System.Text.RegularExpressions;

namespace OmniRAG.Infrastructure.Chunking;

/// <summary>
/// Sentence-based chunking strategy.
/// Chunks by complete sentences to preserve semantic boundaries.
/// </summary>
public sealed class SentenceTextChunker : ITextChunker
{
    private readonly int targetChunkSize;
    private readonly ILogger<SentenceTextChunker>? logger;
    private static readonly Regex SentencePattern = new(
        @"(?<=[.!?])\s+(?=[A-Z])",
        RegexOptions.Compiled);

    public SentenceTextChunker(int targetChunkSize = 512, ILogger<SentenceTextChunker>? logger = null)
    {
        if (targetChunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetChunkSize), "Target chunk size must be positive.");

        this.targetChunkSize = targetChunkSize;
        this.logger = logger;
        
        this.logger?.LogInformation(
            "SentenceTextChunker initialized with target size: {TargetChunkSize} tokens",
            targetChunkSize);
    }

    public IReadOnlyList<TextChunk> ChunkText(
        string text, 
        int pageNumber, 
        IReadOnlyList<string> headings)
    {
        this.logger?.LogDebug(
            "Chunking text for page {PageNumber} using sentence strategy, text length: {TextLength}",
            pageNumber,
            text?.Length ?? 0);

        List<TextChunk> chunks = new List<TextChunk>();
        
        if (string.IsNullOrWhiteSpace(text))
        {
            this.logger?.LogWarning("Empty or null text provided for chunking on page {PageNumber}", pageNumber);
            return chunks;
        }

        try
        {

        string currentSectionTitle = headings.FirstOrDefault() ?? "Introduction";
        
        // Split text into sentences
        List<string> sentences = SentencePattern.Split(text)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        List<string> currentChunk = new List<string>();
        int currentTokens = 0;
        int startIndex = 0;

        foreach (string sentence in sentences)
        {
            int sentenceTokens = EstimateTokens(sentence);

            // Update section title if sentence contains a heading
            string? matchingHeading = headings.FirstOrDefault(h => 
                sentence.Contains(h, StringComparison.OrdinalIgnoreCase));
            if (matchingHeading != null)
            {
                currentSectionTitle = matchingHeading;
            }

            // If adding this sentence would exceed target, create a chunk
            if (currentTokens + sentenceTokens > targetChunkSize && currentChunk.Count > 0)
            {
                string content = string.Join(" ", currentChunk);
                chunks.Add(new TextChunk(
                    content,
                    pageNumber,
                    currentSectionTitle,
                    startIndex,
                    startIndex + content.Length));

                startIndex += content.Length + 1; // +1 for space
                currentChunk.Clear();
                currentTokens = 0;
            }

            currentChunk.Add(sentence);
            currentTokens += sentenceTokens;
        }

        // Add remaining sentences as final chunk
        if (currentChunk.Count > 0)
        {
            string content = string.Join(" ", currentChunk);
            chunks.Add(new TextChunk(
                content,
                pageNumber,
                currentSectionTitle,
                startIndex,
                startIndex + content.Length));
        }

            this.logger?.LogDebug(
                "Created {ChunkCount} chunks for page {PageNumber}",
                chunks.Count,
                pageNumber);

            return chunks;
        }
        catch (Exception ex)
        {
            this.logger?.LogError(
                ex,
                "Failed to chunk text for page {PageNumber}: {ErrorMessage}",
                pageNumber,
                ex.Message);
            throw;
        }
    }

    private static int EstimateTokens(string text)
    {
        // Rough approximation: 1 token ≈ 4 characters for English text
        return text.Length / 4;
    }
}
