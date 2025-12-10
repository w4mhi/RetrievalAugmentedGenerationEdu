namespace OmniRAG.Core.Interfaces;

/// <summary>
/// Represents a chunk of text with metadata.
/// </summary>
/// <param name="Content">The text content of the chunk.</param>
/// <param name="PageNumber">The page number where this chunk originated.</param>
/// <param name="SectionTitle">The section/heading title for this chunk.</param>
/// <param name="StartIndex">Character start index in the original document.</param>
/// <param name="EndIndex">Character end index in the original document.</param>
public record TextChunk(
    string Content, 
    int PageNumber, 
    string SectionTitle, 
    int StartIndex, 
    int EndIndex);
