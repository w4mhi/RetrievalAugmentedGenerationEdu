namespace OmniRAG.Infrastructure.Resilience;

/// <summary>
/// Configuration settings for resilience policies.
/// Follows Configuration Object pattern from Clean Architecture.
/// </summary>
public sealed class ResilienceConfiguration
{
    public PythonNetSettings PythonNet { get; set; } = new();
    public VectorStoreSettings VectorStore { get; set; } = new();
    public LanguageModelSettings LanguageModel { get; set; } = new();
    public DocumentLoaderSettings DocumentLoader { get; set; } = new();

    public sealed class PythonNetSettings
    {
        public int RetryCount { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 30;
        public int CircuitBreakerFailureThreshold { get; set; } = 3;
        public int CircuitBreakerDurationSeconds { get; set; } = 60;
    }

    public sealed class VectorStoreSettings
    {
        public int RetryCount { get; set; } = 5;
        public int CircuitBreakerFailureThreshold { get; set; } = 5;
        public int CircuitBreakerDurationSeconds { get; set; } = 120;
    }

    public sealed class LanguageModelSettings
    {
        public int TimeoutSeconds { get; set; } = 60;
        public string FallbackMessage { get; set; } = "[LLM unavailable] The language model is currently unavailable. Please try again later.";
    }

    public sealed class DocumentLoaderSettings
    {
        public bool EnablePerFileRecovery { get; set; } = true;
    }
}
