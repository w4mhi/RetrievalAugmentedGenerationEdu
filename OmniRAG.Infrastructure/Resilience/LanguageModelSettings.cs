namespace OmniRAG.Infrastructure.Resilience;

public sealed class LanguageModelSettings
{
    public int TimeoutSeconds { get; set; } = 60;
    public string FallbackMessage { get; set; } = "[LLM unavailable] The language model is currently unavailable. Please try again later.";
}
