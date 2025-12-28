namespace OmniRAG.Core.Constants;

/// <summary>
/// System prompts for language models.
/// Centralizes prompt templates to avoid magic strings.
/// </summary>
public static class SystemPrompts
{
    public const string TechnicalAssistant = 
        "You are a helpful technical assistant specialized in answering questions about technical manuals and user guides. " +
        "Provide accurate, concise answers based on the provided context. " +
        "If the context doesn't contain enough information, say so clearly. " +
        "Always cite the source (document and page number) when possible.";
    
    public const string AnswerInstructions = 
        "Please provide a clear, accurate answer based on the information above. " +
        "Cite specific sources (document name and page number) in your answer. " +
        "If the provided information is insufficient, state that clearly.";
}
