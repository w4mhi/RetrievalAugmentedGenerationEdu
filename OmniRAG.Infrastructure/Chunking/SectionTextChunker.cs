using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

using OmniRAG.Core.Interfaces;
using OmniRAG.Core.Models;

namespace OmniRAG.Infrastructure.Chunking;

/// <summary>
/// Section-based chunking strategy.
/// Chunks by document sections/headings to preserve full context.
/// </summary>
public sealed class SectionTextChunker : ITextChunker
{
    private readonly ILogger<SectionTextChunker>? logger;

    public SectionTextChunker(ILogger<SectionTextChunker>? logger = null)
    {
        this.logger = logger;
        this.logger?.LogInformation("SectionTextChunker initialized");
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
            }        // If no headings, treat entire page as one section
        if (headings == null || headings.Count == 0)
        {
            chunks.Add(new TextChunk(
                text,
                pageNumber,
                "Introduction",
                0,
                text.Length));
            return chunks;
        }

        string currentSectionTitle = headings.First();
        List<string> currentContent = new List<string>();
        int startIndex = 0;
        string[] lines = text.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            
            // Check if this line is a heading
            string? matchingHeading = headings.FirstOrDefault(h => 
                trimmedLine.Equals(h, StringComparison.OrdinalIgnoreCase) ||
                trimmedLine.StartsWith(h, StringComparison.OrdinalIgnoreCase));

            if (matchingHeading != null && currentContent.Count > 0)
            {
                // Save previous section as a chunk
                string content = string.Join("\n", currentContent);
                chunks.Add(new TextChunk(
                    content,
                    pageNumber,
                    currentSectionTitle,
                    startIndex,
                    startIndex + content.Length));

                // Start new section
                startIndex += content.Length + 1;
                currentSectionTitle = matchingHeading;
                currentContent.Clear();
            }

            currentContent.Add(line);
        }

            // Add final section
            if (currentContent.Count > 0)
            {
                string content = string.Join("\n", currentContent);
                chunks.Add(new TextChunk(
                    content,
                    pageNumber,
                    currentSectionTitle,
                    startIndex,
                    startIndex + content.Length));
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
}
