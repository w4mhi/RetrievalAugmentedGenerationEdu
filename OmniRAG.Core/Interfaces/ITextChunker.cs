namespace OmniRAG.Core.Interfaces;

using OmniRAG.Core.Models;

/// <summary>
/// Interface for text chunking strategies.
/// Strategy Pattern: Defines contract for different chunking algorithms.
/// Interface Segregation Principle: Focused on text chunking only.
/// </summary>
public interface ITextChunker
{
    /// <summary>
    /// Chunks text into smaller segments based on the implemented strategy.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="pageNumber">The page number in the source document.</param>
    /// <param name="headings">Available section headings for context.</param>
    /// <returns>A read-only list of text chunks.</returns>
    IReadOnlyList<TextChunk> ChunkText(
        string text, 
        int pageNumber, 
        IReadOnlyList<string> headings);
}
