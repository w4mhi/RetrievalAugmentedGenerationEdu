using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

using OmniRAG.Core.Interfaces;
using OmniRAG.Core.Models;

namespace OmniRAG.Infrastructure.Chunking;

/// <summary>
/// Semantic chunking strategy for technical manuals.
/// Preserves context by chunking based on paragraphs with overlap.
/// </summary>
public sealed class SemanticTextChunker : ITextChunker
{
    private readonly int targetChunkSize;
    private readonly int overlapSize;
    private readonly ILogger<SemanticTextChunker>? logger;

    public SemanticTextChunker(int targetChunkSize = 512, int overlapSize = 100, ILogger<SemanticTextChunker>? logger = null)
    {
        if (targetChunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetChunkSize), "Target chunk size must be positive.");
        if (overlapSize < 0)
            throw new ArgumentOutOfRangeException(nameof(overlapSize), "Overlap size cannot be negative.");
        if (overlapSize >= targetChunkSize)
            throw new ArgumentException("Overlap size must be less than target chunk size.");

        this.targetChunkSize = targetChunkSize;
        this.overlapSize = overlapSize;
        this.logger = logger;
        this.logger?.LogInformation("SemanticTextChunker initialized with targetChunkSize={TargetChunkSize}, overlapSize={OverlapSize}", targetChunkSize, overlapSize);
    }

    public IReadOnlyList<TextChunk> ChunkText(
        string text,
        int pageNumber,
        IReadOnlyList<string> headings)
    {
        this.logger?.LogDebug("ChunkText called with textLength={TextLength}, pageNumber={PageNumber}, headingsCount={HeadingsCount}", text?.Length ?? 0, pageNumber, headings?.Count ?? 0);

        try
        {
            List<TextChunk> chunks = new List<TextChunk>();

            if (string.IsNullOrWhiteSpace(text))
            {
                this.logger?.LogWarning("Empty text provided for chunking on page {PageNumber}", pageNumber);
                return chunks;
            }

            // Split by paragraphs first
            string[] paragraphs = text.Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries);        List<string> currentChunk = new List<string>();
        int currentSize = 0;
        int chunkStartIndex = 0;
        string currentSectionTitle = (headings != null && headings.Count > 0) ? headings.FirstOrDefault() ?? "Introduction" : "Introduction";

        for (int i = 0; i < paragraphs.Length; i++)
        {
            string paragraph = paragraphs[i].Trim();
            int paragraphSize = EstimateTokens(paragraph);

            // Check if this paragraph is a heading
            string? matchingHeading = headings != null ? headings.FirstOrDefault(h =>
                paragraph.Contains(h, StringComparison.OrdinalIgnoreCase)) : null;
            if (matchingHeading != null)
            {
                currentSectionTitle = matchingHeading;
            }

            // If adding this paragraph exceeds target size, create a chunk
            if (currentSize + paragraphSize > targetChunkSize && currentChunk.Count > 0)
            {
                string chunkContent = string.Join("\n\n", currentChunk);
                chunks.Add(new TextChunk(
                    chunkContent,
                    pageNumber,
                    currentSectionTitle,
                    chunkStartIndex,
                    chunkStartIndex + chunkContent.Length));

                // Keep last paragraph for overlap
                currentChunk.Clear();
                if (EstimateTokens(paragraph) < overlapSize)
                {
                    currentChunk.Add(paragraph);
                    currentSize = paragraphSize;
                }
                else
                {
                    currentSize = 0;
                }
                
                chunkStartIndex += chunkContent.Length;
            }
            else
            {
                currentChunk.Add(paragraph);
                currentSize += paragraphSize;
            }
        }

            // Add remaining content as final chunk
            if (currentChunk.Count > 0)
            {
                string chunkContent = string.Join("\n\n", currentChunk);
                chunks.Add(new TextChunk(
                    chunkContent,
                    pageNumber,
                    currentSectionTitle,
                    chunkStartIndex,
                    chunkStartIndex + chunkContent.Length));
            }

            this.logger?.LogDebug("Chunking completed with {ChunkCount} chunks created", chunks.Count);
            return chunks;
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "Error during text chunking: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    private static int EstimateTokens(string text)
    {
        // Rough approximation: 1 token ≈ 4 characters for English text
        return text.Length / 4;
    }
}
