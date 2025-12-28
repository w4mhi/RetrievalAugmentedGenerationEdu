using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

using OmniRAG.Core.Interfaces;
using OmniRAG.Core.Models;

namespace OmniRAG.Infrastructure.Chunking;

/// <summary>
/// Fixed-size chunking strategy.
/// Creates uniform chunks of fixed token length with optional overlap.
/// </summary>
public sealed class FixedSizeTextChunker : ITextChunker
{
    private readonly int chunkSize;
    private readonly int overlapSize;
    private readonly ILogger<FixedSizeTextChunker>? logger;

    public FixedSizeTextChunker(int chunkSize = 512, int overlapSize = 50, ILogger<FixedSizeTextChunker>? logger = null)
    {
        if (chunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be positive.");
        if (overlapSize < 0)
            throw new ArgumentOutOfRangeException(nameof(overlapSize), "Overlap size cannot be negative.");
        if (overlapSize >= chunkSize)
            throw new ArgumentException("Overlap size must be less than chunk size.");

        this.chunkSize = chunkSize;
        this.overlapSize = overlapSize;
        this.logger = logger;
        
        this.logger?.LogInformation(
            "FixedSizeTextChunker initialized with chunk size: {ChunkSize}, overlap: {OverlapSize}",
            chunkSize,
            overlapSize);
    }

    public IReadOnlyList<TextChunk> ChunkText(
        string text, 
        int pageNumber, 
        IReadOnlyList<string> headings)
    {
        this.logger?.LogDebug(
            "Chunking text for page {PageNumber} using fixed-size strategy, text length: {TextLength}",
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
            string[] words = SplitIntoWords(text);
            chunks = BuildChunksFromWords(words, pageNumber, headings, currentSectionTitle);

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

    /// <summary>
    /// Splits text into words array.
    /// Reduces method complexity (Rule 8).
    /// </summary>
    private static string[] SplitIntoWords(string text)
    {
        return text.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Builds chunks from words array with overlap.
    /// Reduces method complexity (Rule 8).
    /// </summary>
    private List<TextChunk> BuildChunksFromWords(
        string[] words,
        int pageNumber,
        IReadOnlyList<string> headings,
        string currentSectionTitle)
    {
        List<TextChunk> chunks = new List<TextChunk>();
        List<string> currentChunk = new List<string>();
        int startIndex = 0;

        for (int i = 0; i < words.Length; i++)
        {
            currentChunk.Add(words[i]);
            int estimatedTokens = (int)(currentChunk.Count * 1.3);
            string chunkText = string.Join(" ", currentChunk);
            currentSectionTitle = UpdateSectionTitle(chunkText, headings, currentSectionTitle);

            if (estimatedTokens >= this.chunkSize || i == words.Length - 1)
            {
                chunks.Add(CreateChunk(chunkText, pageNumber, currentSectionTitle, startIndex));

                if (i < words.Length - 1)
                {
                    startIndex += chunkText.Length;
                    currentChunk = GetOverlapWords(currentChunk);
                }
            }
        }

        return chunks;
    }

    private static string UpdateSectionTitle(string chunkText, IReadOnlyList<string> headings, string currentSectionTitle)
    {
        string? matchingHeading = headings.FirstOrDefault(h => 
            chunkText.Contains(h, StringComparison.OrdinalIgnoreCase));
        return matchingHeading ?? currentSectionTitle;
    }

    private static TextChunk CreateChunk(string content, int pageNumber, string sectionTitle, int startIndex)
    {
        return new TextChunk(
            content,
            pageNumber,
            sectionTitle,
            startIndex,
            startIndex + content.Length);
    }

    private List<string> GetOverlapWords(List<string> currentChunk)
    {
        int overlapWords = (int)(this.overlapSize / 1.3);
        int keepWords = Math.Min(overlapWords, currentChunk.Count);
        return currentChunk.Skip(currentChunk.Count - keepWords).ToList();
    }
}
