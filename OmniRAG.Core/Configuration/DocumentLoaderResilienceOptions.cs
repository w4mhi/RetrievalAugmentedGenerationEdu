namespace OmniRAG.Core.Configuration;

/// <summary>
/// Document loader resilience configuration.
/// </summary>
public class DocumentLoaderResilienceOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to enable per-file error recovery during batch document loading.
    /// </summary>
    public bool EnablePerFileRecovery { get; set; } = true;
}
