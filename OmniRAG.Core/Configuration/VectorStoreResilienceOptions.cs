using System.ComponentModel.DataAnnotations;

namespace OmniRAG.Core.Configuration;

/// <summary>
/// Vector store resilience configuration.
/// </summary>
public class VectorStoreResilienceOptions
{
    [Range(0, 10, ErrorMessage = "RetryCount must be between 0 and 10")]
    public int RetryCount { get; set; } = 5;

    [Range(1, 100, ErrorMessage = "CircuitBreakerFailureThreshold must be between 1 and 100")]
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    [Range(1, 600, ErrorMessage = "CircuitBreakerDurationSeconds must be between 1 and 600")]
    public int CircuitBreakerDurationSeconds { get; set; } = 60;
}
