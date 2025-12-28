namespace OmniRAG.Core.Constants;

/// <summary>
/// Collection names and identifiers for data stores.
/// </summary>
public static class CollectionNames
{
    public const string RadioExpertDocuments = "radio_expert_documents";
}

/// <summary>
/// Fallback messages for error scenarios.
/// </summary>
public static class FallbackMessages
{
    public const string LlmUnavailable = "[LLM unavailable] Unable to generate response.";
    public const string LlmUnavailableRetryLater = "[LLM unavailable] The language model is currently unavailable. Please try again later.";
    public const string NoRelevantInformation = "I couldn't find any relevant information in the documentation to answer your question.";
    public const string NoRelevantInformationSimple = "I couldn't find any relevant information in the documentation.";
    public const string ConfigurePhi4Note = "(Note: LLM not configured. Showing retrieved chunks only. Configure Phi-4 in appsettings.json for enhanced answers.)";
}
