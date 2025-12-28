namespace OmniRAG.Infrastructure.Resilience;

public sealed class PythonNetSettings
{
    public int RetryCount { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 30;
    public int CircuitBreakerFailureThreshold { get; set; } = 3;
    public int CircuitBreakerDurationSeconds { get; set; } = 60;
}
