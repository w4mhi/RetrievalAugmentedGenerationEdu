using System;

using Microsoft.Extensions.Logging;

using OmniRAG.Core.Interfaces;
using OmniRAG.Core.Models;

namespace OmniRAG.Infrastructure.Chunking;

/// <summary>
/// Factory for creating text chunker instances based on strategy.
/// Factory Pattern: Encapsulates chunker creation logic.
/// </summary>
public static class TextChunkerFactory
{
    /// <summary>
    /// Creates a text chunker instance for the specified strategy.
    /// </summary>
    /// <param name="strategy">The chunking strategy to use.</param>
    /// <param name="targetChunkSize">Target chunk size in tokens (for applicable strategies).</param>
    /// <param name="overlapSize">Overlap size in tokens (for applicable strategies).</param>
    /// <param name="loggerFactory">Optional logger factory for creating typed loggers.</param>
    /// <returns>An ITextChunker implementation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when strategy is unknown.</exception>
    public static ITextChunker Create(
        ChunkingStrategy strategy,
        int targetChunkSize = 512,
        int overlapSize = 100,
        ILoggerFactory? loggerFactory = null)
    {
        return strategy switch
        {
            ChunkingStrategy.Semantic => new SemanticTextChunker(
                targetChunkSize, 
                overlapSize, 
                loggerFactory?.CreateLogger<SemanticTextChunker>()),
            ChunkingStrategy.Fixed => new FixedSizeTextChunker(
                targetChunkSize, 
                overlapSize / 2, 
                loggerFactory?.CreateLogger<FixedSizeTextChunker>()),
            ChunkingStrategy.Sentence => new SentenceTextChunker(
                targetChunkSize, 
                loggerFactory?.CreateLogger<SentenceTextChunker>()),
            ChunkingStrategy.Section => new SectionTextChunker(
                loggerFactory?.CreateLogger<SectionTextChunker>()),
            _ => throw new ArgumentOutOfRangeException(
                nameof(strategy), 
                strategy, 
                $"Unknown chunking strategy: {strategy}")
        };
    }
}
