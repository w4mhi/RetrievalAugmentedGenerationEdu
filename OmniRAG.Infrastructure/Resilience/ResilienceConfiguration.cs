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
}
