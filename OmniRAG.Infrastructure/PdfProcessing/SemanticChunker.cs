using Microsoft.Extensions.Logging;
using OmniRAG.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

using OmniRAG.Core.Models;

namespace OmniRAG.Infrastructure.PdfProcessing;

/// <summary>
/// Semantic chunking strategy for technical manuals.
/// Preserves context by chunking based on sections with overlap.
/// </summary>
public sealed class SemanticChunker
{
    private const int TargetChunkSize = 512; // tokens (approximated by characters/4)
    private const int OverlapSize = 100;
    private readonly ILogger<SemanticChunker>? logger;

    public SemanticChunker(ILogger<SemanticChunker>? logger = null)
    {
        this.logger = logger;
        this.logger?.LogInformation("SemanticChunker initialized with targetChunkSize={TargetChunkSize}, overlapSize={OverlapSize}", TargetChunkSize, OverlapSize);
    }

    public IReadOnlyList<TextChunk> ChunkText(string text, int pageNumber, IReadOnlyList<string> headings)
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
        string[] paragraphs = text.Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries);
        
        List<string> currentChunk = new List<string>();
        int currentSize = 0;
        int chunkStartIndex = 0;
        string currentSectionTitle = (headings?.FirstOrDefault()) ?? "Introduction";

        for (int i = 0; i < paragraphs.Length; i++)
        {
            string paragraph = paragraphs[i].Trim();
            int paragraphSize = EstimateTokens(paragraph);

            // Check if this paragraph is a heading
            string? matchingHeading = headings?.FirstOrDefault(h => paragraph.Contains(h, StringComparison.OrdinalIgnoreCase));
            if (matchingHeading != null)
            {
                currentSectionTitle = matchingHeading;
            }

            // If adding this paragraph exceeds target size, create a chunk
            if (currentSize + paragraphSize > TargetChunkSize && currentChunk.Count > 0)
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
                if (EstimateTokens(paragraph) < OverlapSize)
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
