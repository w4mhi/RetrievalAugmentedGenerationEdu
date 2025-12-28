namespace OmniRAG.Core.Constants;

/// <summary>
/// Display messages for user interface.
/// </summary>
public static class DisplayMessages
{
    public const string Phi4DisabledInConfiguration = "ℹ Phi-4 disabled in configuration (retrieval-only mode)";
    public const string Phi4ModelPathNotConfigured = "⚠ Phi-4 model path not configured";
    public const string RunningInRetrievalOnlyMode = "  Running in retrieval-only mode";
    public const string Phi4LanguageModelEnabled = "✓ Phi-4 language model enabled";
    public const string Phi4InitializationFailed = "⚠ Phi-4 initialization failed: {0}";
    public const string DocumentMonitoringDisabled = "ℹ Document monitoring disabled";
    public const string DocumentMonitoringEnabled = "✓ Document monitoring enabled";
}
