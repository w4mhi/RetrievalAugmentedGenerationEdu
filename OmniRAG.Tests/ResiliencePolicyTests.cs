using FluentAssertions;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using OmniRAG.Infrastructure.Resilience;

namespace OmniRAG.Tests;

/// <summary>
/// Integration tests for resilience policies.
/// Tests verify circuit breaker, retry, timeout, and fallback behavior.
/// </summary>
public sealed class ResiliencePolicyTests
{
    [Fact]
    public async Task PythonNetPipeline_ShouldRetry_OnTransientFailure()
    {
        // Arrange
        int attemptCount = 0;
        IAsyncPolicy<int> policy = ResiliencePolicies.CreatePythonNetPipeline<int>(
            logger: null,
            operationName: "TestOperation",
            retryCount: 3);

        // Act
        int result = await policy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                await Task.Delay(10);
                throw new IOException("Simulated transient failure");
            }
            return 42;
        });

        // Assert
        Assert.Equal(42, result);
        Assert.Equal(3, attemptCount); // Should succeed on 3rd attempt after 2 retries
    }

    [Fact]
    public async Task PythonNetPipeline_ShouldTimeout_OnSlowOperation()
    {
        // Arrange
        IAsyncPolicy<int> policy = ResiliencePolicies.CreatePythonNetPipeline<int>(
            logger: null,
            operationName: "TestOperation",
            timeoutSeconds: 1);

        // Act
        Func<Task> act = async () =>
        {
            await policy.ExecuteAsync(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5)); // Exceeds 1s timeout
                return 42;
            });
        };

        // Assert
        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }

    [Fact]
    public async Task LanguageModelPipeline_ShouldReturnFallback_OnFailure()
    {
        // Arrange
        string expectedFallback = "Fallback response";
        IAsyncPolicy<string> policy = ResiliencePolicies.CreateLanguageModelPipeline(
            logger: null,
            fallbackValue: expectedFallback,
            operationName: "TestLLM");

        // Act
        string result = await policy.ExecuteAsync(() => 
            throw new InvalidOperationException("LLM failure"));

        // Assert
        Assert.Equal(expectedFallback, result);
    }

    [Fact]
    public async Task LanguageModelPipeline_ShouldTimeout_OnSlowInference()
    {
        // Arrange
        string fallbackMessage = "Timeout fallback";
        IAsyncPolicy<string> policy = ResiliencePolicies.CreateLanguageModelPipeline(
            logger: null,
            fallbackValue: fallbackMessage,
            operationName: "TestLLM",
            timeoutSeconds: 1);

        // Act
        string result = await policy.ExecuteAsync(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5)); // Exceeds 1s timeout
            return "Should not reach here";
        });

        // Assert - Timeout triggers fallback
        Assert.Equal(fallbackMessage, result);
    }

    [Fact]
    public async Task VectorStorePipeline_ShouldRetry_OnIOException()
    {
        // Arrange
        int attemptCount = 0;
        IAsyncPolicy<string> policy = ResiliencePolicies.CreateVectorStorePipeline<string>(
            logger: null,
            operationName: "TestVectorStore",
            retryCount: 5);

        // Act
        string result = await policy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 4)
            {
                await Task.Delay(10);
                throw new IOException("Simulated disk I/O failure");
            }
            return "Success";
        });

        // Assert
        Assert.Equal("Success", result);
        Assert.Equal(4, attemptCount); // Should succeed on 4th attempt after 3 retries
    }

    [Fact]
    public async Task VectorStorePipeline_ShouldOpenCircuitBreaker_AfterThresholdFailures()
    {
        // Arrange
        IAsyncPolicy<string> policy = ResiliencePolicies.CreateVectorStorePipeline<string>(
            logger: null,
            operationName: "TestVectorStore",
            circuitBreakerFailures: 3,
            retryCount: 0); // Disable retry to test circuit breaker directly

        // Act - Trigger circuit breaker by exceeding failure threshold
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await policy.ExecuteAsync(() => throw new IOException("Persistent failure"));
            }
            catch (IOException)
            {
                // Expected
            }
        }

        // Assert - Circuit should be open, next call should throw BrokenCircuitException
        Func<Task> act = async () =>
        {
            await policy.ExecuteAsync(() => Task.FromResult("Should not execute"));
        };
        
        await act.Should().ThrowAsync<BrokenCircuitException>();
    }

    [Fact]
    public async Task DocumentLoaderPipeline_ShouldReturnFallback_OnFileProcessingError()
    {
        // Arrange
        List<string> expectedFallback = new List<string>();
        IAsyncPolicy<List<string>> policy = ResiliencePolicies.CreateDocumentLoaderPipeline<List<string>>(
            logger: null,
            fallbackValue: expectedFallback,
            operationName: "TestDocumentLoader");

        // Act
        List<string> result = await policy.ExecuteAsync(
            (context) => throw new FileNotFoundException("File not found"),
            new Dictionary<string, object> { ["FilePath"] = "test.pdf" });

        // Assert
        Assert.Same(expectedFallback, result);
    }

    [Fact]
    public async Task DocumentLoaderPipeline_ShouldIncludeFilePathInContext()
    {
        // Arrange
        string? capturedFilePath = null;
        List<string> fallback = new List<string>();
        
        IAsyncPolicy<List<string>> policy = ResiliencePolicies.CreateDocumentLoaderPipeline<List<string>>(
            logger: null,
            fallbackValue: fallback,
            operationName: "TestDocumentLoader");

        // Act
        await policy.ExecuteAsync(
            (context) =>
            {
                if (context.TryGetValue("FilePath", out object? value))
                {
                    capturedFilePath = value as string;
                }
                throw new IOException("Simulated error");
            },
            new Dictionary<string, object> { ["FilePath"] = "important.pdf" });

        // Assert
        Assert.Equal("important.pdf", capturedFilePath);
    }

    [Fact]
    public async Task PythonNetPipeline_CustomConfiguration_ShouldUseProvidedValues()
    {
        // Arrange
        int attemptCount = 0;
        IAsyncPolicy<int> policy = ResiliencePolicies.CreatePythonNetPipeline<int>(
            logger: null,
            operationName: "CustomConfig",
            retryCount: 2,  // Custom: only 2 retries
            timeoutSeconds: 5,
            circuitBreakerFailures: 10);

        // Act
        int result = await policy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                await Task.Delay(10);
                throw new IOException("Failure");
            }
            return 100;
        });

        // Assert
        Assert.Equal(100, result);
        Assert.Equal(3, attemptCount); // Initial + 2 retries = 3 total attempts
    }
}
