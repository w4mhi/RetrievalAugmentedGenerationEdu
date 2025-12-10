using System.ComponentModel.DataAnnotations;

namespace OmniRAG.Core.Configuration;

/// <summary>
/// PythonNet resilience configuration.
/// </summary>
public class PythonNetResilienceOptions
{
    [Range(0, 10, ErrorMessage = "RetryCount must be between 0 and 10")]
    public int RetryCount { get; set; } = 3;

    [Range(1, 300, ErrorMessage = "TimeoutSeconds must be between 1 and 300")]
    public int TimeoutSeconds { get; set; } = 30;

    [Range(1, 100, ErrorMessage = "CircuitBreakerFailureThreshold must be between 1 and 100")]
    public int CircuitBreakerFailureThreshold { get; set; } = 3;

    [Range(1, 600, ErrorMessage = "CircuitBreakerDurationSeconds must be between 1 and 600")]
    public int CircuitBreakerDurationSeconds { get; set; } = 60;
}
