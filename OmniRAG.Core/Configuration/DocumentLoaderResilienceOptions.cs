namespace OmniRAG.Core.Configuration;

/// <summary>
/// Document loader resilience configuration.
/// </summary>
public class DocumentLoaderResilienceOptions
{
    public bool EnablePerFileRecovery { get; set; } = true;
}
