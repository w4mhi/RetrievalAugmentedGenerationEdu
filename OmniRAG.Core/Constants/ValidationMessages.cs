namespace OmniRAG.Core.Constants;

/// <summary>
/// Validation error messages for command-line parameters.
/// </summary>
public static class ValidationMessages
{
    public const string ChunkSizeMustBePositive = "Error: Chunk size must be positive";
    public const string OverlapCannotBeNegative = "Error: Overlap cannot be negative";
    public const string OverlapMustBeLessThanChunkSize = "Error: Overlap must be less than chunk size";
    public const string TopKMustBeBetween1And50 = "Top-K must be between 1 and 50";
    public const string SimilarityThresholdMustBeBetween0And1 = "Similarity threshold must be between 0.0 and 1.0";
}
