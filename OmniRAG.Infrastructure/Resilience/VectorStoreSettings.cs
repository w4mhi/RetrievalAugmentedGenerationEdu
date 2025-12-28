namespace OmniRAG.Infrastructure.Resilience;

public sealed class VectorStoreSettings
{
    public int RetryCount { get; set; } = 5;
    public int CircuitBreakerFailureThreshold { get; set; } = 5;
    public int CircuitBreakerDurationSeconds { get; set; } = 120;
}
