using System;
using System.IO;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

using Python.Runtime;

using OmniRAG.Core.Constants;

namespace OmniRAG.Infrastructure.Resilience;

/// <summary>
/// Centralized factory for creating resilience policies using Polly.
/// Factory Pattern: Encapsulates policy creation logic.
/// Single Responsibility: Manages all resilience policies for OmniRAG.
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Creates a resilience pipeline for Python.NET operations (embeddings and vector store).
    /// Combines: Circuit Breaker (3 failures, 1 min break) + Retry (3x exponential backoff) + Timeout (30s).
    /// </summary>
    /// <typeparam name="TResult">The result type of the operation.</typeparam>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <param name="operationName">Name of the operation for logging context.</param>
    /// <param name="retryCount">Number of retry attempts (default: 3).</param>
    /// <param name="timeoutSeconds">Timeout in seconds (default: 30).</param>
    /// <param name="circuitBreakerFailures">Failures before circuit breaker opens (default: 3).</param>
    /// <param name="circuitBreakerDurationSeconds">Circuit breaker duration in seconds (default: 60).</param>
    /// <returns>A composed resilience pipeline.</returns>
    public static IAsyncPolicy<TResult> CreatePythonNetPipeline<TResult>(
        ILogger? logger = null,
        string operationName = "PythonNet",
        int retryCount = DefaultValues.DefaultRetryCount,
        int timeoutSeconds = DefaultValues.DefaultTimeoutSeconds,
        int circuitBreakerFailures = DefaultValues.DefaultCircuitBreakerFailures,
        int circuitBreakerDurationSeconds = DefaultValues.DefaultCircuitBreakerDurationSeconds)
    {
        AsyncCircuitBreakerPolicy circuitBreaker = Policy
            .Handle<PythonException>()
            .Or<InvalidOperationException>()
            .Or<TimeoutException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: circuitBreakerFailures,
                durationOfBreak: TimeSpan.FromSeconds(circuitBreakerDurationSeconds),
                onBreak: (exception, duration) =>
                {
                    logger?.LogError(exception,
                        LogMessages.CircuitBreakerOpened,
                        operationName,
                        duration.TotalSeconds,
                        exception.GetType().Name);
                },
                onReset: () =>
                {
                    logger?.LogInformation(LogMessages.CircuitBreakerReset, operationName);
                },
                onHalfOpen: () =>
                {
                    logger?.LogInformation(LogMessages.CircuitBreakerHalfOpen, operationName);
                });

        AsyncRetryPolicy retryPolicy = Policy
            .Handle<PythonException>()
            .Or<IOException>()
            .Or<InvalidOperationException>()
            .WaitAndRetryAsync(
                retryCount: retryCount,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, timespan, currentRetry, context) =>
                {
                    logger?.LogWarning(exception,
                        LogMessages.RetryAttempt,
                        operationName,
                        currentRetry,
                        retryCount,
                        timespan.TotalSeconds,
                        exception.Message);
                });

        AsyncTimeoutPolicy timeoutPolicy = Policy.TimeoutAsync(
            TimeSpan.FromSeconds(timeoutSeconds),
            TimeoutStrategy.Pessimistic,
            onTimeoutAsync: (context, timespan, task) =>
            {
                logger?.LogWarning(
                    LogMessages.OperationTimedOut,
                    operationName,
                    timespan.TotalSeconds);
                return Task.CompletedTask;
            });

        return Policy.WrapAsync<TResult>(
            circuitBreaker.AsAsyncPolicy<TResult>(),
            retryPolicy.AsAsyncPolicy<TResult>(),
            timeoutPolicy.AsAsyncPolicy<TResult>());
    }

    /// <summary>
    /// Creates a resilience pipeline for LLM operations (Phi-4 generation).
    /// Combines: Timeout (60s) + Fallback with graceful degradation.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <param name="fallbackValue">Fallback value if operation fails.</param>
    /// <param name="operationName">Name of the operation for logging context.</param>
    /// <param name="timeoutSeconds">Timeout in seconds (default: 60).</param>
    /// <returns>A composed resilience pipeline.</returns>
    public static IAsyncPolicy<string> CreateLanguageModelPipeline(
        ILogger? logger = null,
        string? fallbackValue = null,
        string operationName = "LanguageModel",
        int timeoutSeconds = DefaultValues.LanguageModelTimeoutSeconds)
    {
        AsyncTimeoutPolicy timeoutPolicy = Policy.TimeoutAsync(
            TimeSpan.FromSeconds(timeoutSeconds),
            TimeoutStrategy.Pessimistic,
            onTimeoutAsync: (context, timespan, task) =>
            {
                logger?.LogWarning(
                    LogMessages.LlmInferenceTimedOut,
                    operationName,
                    timespan.TotalSeconds);
                return Task.CompletedTask;
            });

        IAsyncPolicy<string> fallbackPolicy = Policy<string>
            .Handle<Exception>()
            .FallbackAsync(
                fallbackValue: fallbackValue ?? FallbackMessages.LlmUnavailable,
                onFallbackAsync: (exception, context) =>
                {
                    logger?.LogWarning(exception.Exception,
                        LogMessages.LlmOperationFailed,
                        operationName,
                        exception.Exception.Message);
                    return Task.CompletedTask;
                });

        return Policy.WrapAsync(
            fallbackPolicy,
            timeoutPolicy.AsAsyncPolicy<string>());
    }

    /// <summary>
    /// Creates a resilience pipeline for vector store I/O operations.
    /// Combines: Circuit Breaker (5 failures, 2 min break) + Retry (5x exponential backoff).
    /// </summary>
    /// <typeparam name="TResult">The result type of the operation.</typeparam>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <param name="operationName">Name of the operation for logging context.</param>
    /// <param name="retryCount">Number of retry attempts (default: 5).</param>
    /// <param name="circuitBreakerFailures">Failures before circuit breaker opens (default: 5).</param>
    /// <param name="circuitBreakerDurationSeconds">Circuit breaker duration in seconds (default: 120).</param>
    /// <returns>A composed resilience pipeline.</returns>
    public static IAsyncPolicy<TResult> CreateVectorStorePipeline<TResult>(
        ILogger? logger = null,
        string operationName = "VectorStore",
        int retryCount = DefaultValues.VectorStoreRetryCount,
        int circuitBreakerFailures = DefaultValues.VectorStoreCircuitBreakerFailures,
        int circuitBreakerDurationSeconds = DefaultValues.VectorStoreCircuitBreakerDurationSeconds)
    {
        AsyncCircuitBreakerPolicy circuitBreaker = Policy
            .Handle<IOException>()
            .Or<PythonException>()
            .Or<TimeoutException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: circuitBreakerFailures,
                durationOfBreak: TimeSpan.FromSeconds(circuitBreakerDurationSeconds),
                onBreak: (exception, duration) =>
                {
                    logger?.LogError(exception,
                        LogMessages.VectorStoreCircuitBreakerOpened,
                        operationName,
                        duration.TotalSeconds);
                },
                onReset: () =>
                {
                    logger?.LogInformation(LogMessages.VectorStoreCircuitBreakerReset, operationName);
                });

        AsyncRetryPolicy retryPolicy = Policy
            .Handle<IOException>()
            .Or<PythonException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: retryCount,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)),
                onRetry: (exception, timespan, currentRetry, context) =>
                {
                    logger?.LogWarning(exception,
                        LogMessages.VectorStoreRetry,
                        operationName,
                        currentRetry,
                        retryCount,
                        timespan.TotalMilliseconds,
                        exception.Message);
                });

        return Policy.WrapAsync<TResult>(
            circuitBreaker.AsAsyncPolicy<TResult>(),
            retryPolicy.AsAsyncPolicy<TResult>());
    }

    /// <summary>
    /// Creates a resilience pipeline for PDF document loading operations.
    /// Uses: Fallback (per-file graceful degradation).
    /// </summary>
    /// <typeparam name="TResult">The result type of the operation.</typeparam>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <param name="fallbackValue">Fallback value if operation fails.</param>
    /// <param name="operationName">Name of the operation for logging context.</param>
    /// <returns>A resilience pipeline with fallback.</returns>
    public static IAsyncPolicy<TResult> CreateDocumentLoaderPipeline<TResult>(
        ILogger? logger = null,
        TResult? fallbackValue = default,
        string operationName = "DocumentLoader")
    {
        IAsyncPolicy<TResult> fallbackPolicy = Policy<TResult>
            .Handle<Exception>()
            .FallbackAsync(
                fallbackValue: fallbackValue!,
                onFallbackAsync: (exception, context) =>
                {
                    string? filePath = context.TryGetValue("FilePath", out object? value) ? value as string : "unknown";
                    logger?.LogWarning(exception.Exception,
                        LogMessages.DocumentLoadingFailed,
                        operationName,
                        filePath,
                        exception.Exception.Message);
                    return Task.CompletedTask;
                });

        return fallbackPolicy;
    }
}
