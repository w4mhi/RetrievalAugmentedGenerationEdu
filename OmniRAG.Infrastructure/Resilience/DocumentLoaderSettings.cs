namespace OmniRAG.Infrastructure.Resilience;

public sealed class DocumentLoaderSettings
{
    public bool EnablePerFileRecovery { get; set; } = true;
}
