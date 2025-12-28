using System.ComponentModel.DataAnnotations;

namespace OmniRAG.Core.Configuration;

/// <summary>
/// Vector store resilience configuration.
/// </summary>
public class VectorStoreResilienceOptions
{
    /// <summary>
    /// Gets or sets the number of retry attempts for vector store operations.
    /// </summary>
    [Range(0, 10, ErrorMessage = "RetryCount must be between 0 and 10")]
    public int RetryCount { get; set; } = 5;

    /// <summary>
    /// Gets or sets the number of failures before opening the circuit breaker.
    /// </summary>
    [Range(1, 100, ErrorMessage = "CircuitBreakerFailureThreshold must be between 1 and 100")]
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the duration in seconds the circuit breaker stays open.
    /// </summary>
    [Range(1, 600, ErrorMessage = "CircuitBreakerDurationSeconds must be between 1 and 600")]
    public int CircuitBreakerDurationSeconds { get; set; } = 60;
}
