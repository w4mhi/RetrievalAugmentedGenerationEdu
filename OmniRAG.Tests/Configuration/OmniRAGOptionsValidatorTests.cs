using FluentAssertions;
using Microsoft.Extensions.Options;
using OmniRAG.Core.Configuration;
using OmniRAG.Core.Validation;
using Xunit;

namespace OmniRAG.Tests.Configuration;

public class OmniRAGOptionsValidatorTests
{
    [Fact]
    public void Validate_WithValidConfiguration_ReturnsSuccess()
    {
        // Arrange
        OmniRAGOptions options = CreateValidOptions();
        OmniRAGOptionsValidator validator = new OmniRAGOptionsValidator();

        // Act
        ValidateOptionsResult result = validator.Validate(null, options);

        // Assert - if it failed, print the failures for debugging
        if (result.Failed && result.Failures != null)
        {
            foreach (string failure in result.Failures)
            {
                System.Diagnostics.Debug.WriteLine($"Validation failure: {failure}");
            }
        }
        
        result.Succeeded.Should().BeTrue($"Validation should succeed. Failures: {(result.Failures != null ? string.Join(", ", result.Failures) : "none")}");
        result.Failures.Should().BeNull();
    }

    [Fact]
    public void Validate_WithMissingPythonDll_ReturnsFailure()
    {
        // Arrange
        OmniRAGOptions options = CreateValidOptions();
        options.Python.DllPath = "C:\\NonExistent\\python312.dll";
        OmniRAGOptionsValidator validator = new OmniRAGOptionsValidator();

        // Act
        ValidateOptionsResult result = validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Python DLL not found"));
    }

    [Fact]
    public void Validate_WithNoEnabledLanguageModel_ReturnsFailure()
    {
        // Arrange
        OmniRAGOptions options = CreateValidOptions();
        options.LanguageModel.Phi4 = null;
        options.LanguageModel.Mistral = null;
        options.LanguageModel.GPT = null;
        options.LanguageModel.Llama = null;
        OmniRAGOptionsValidator validator = new OmniRAGOptionsValidator();

        // Act
        ValidateOptionsResult result = validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("No language model provider is enabled"));
    }

    [Fact]
    public void Validate_WithRetryCountExceedingCircuitBreaker_ReturnsFailure()
    {
        // Arrange
        OmniRAGOptions options = CreateValidOptions();
        options.Resilience.PythonNet.RetryCount = 10;
        options.Resilience.PythonNet.CircuitBreakerFailureThreshold = 3;
        OmniRAGOptionsValidator validator = new OmniRAGOptionsValidator();

        // Act
        ValidateOptionsResult result = validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("PythonNet RetryCount should not exceed CircuitBreakerFailureThreshold"));
    }

    private static OmniRAGOptions CreateValidOptions()
    {
        // Use paths that exist on the current platform for testing
        // We need files that actually exist on the system
        string pythonDll;
        string pythonHome;
        
        if (OperatingSystem.IsWindows())
        {
            string systemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
            pythonDll = Path.Combine(systemDir, "kernel32.dll"); // Always exists on Windows
            pythonHome = systemDir;
        }
        else if (OperatingSystem.IsMacOS())
        {
            // Use Python framework paths that exist on macOS with Python installed
            pythonDll = "/Library/Frameworks/Python.framework/Versions/Current/lib/libpython3.13.dylib";
            pythonHome = "/Library/Frameworks/Python.framework/Versions/Current";
            
            // Fallback to common Homebrew location
            if (!File.Exists(pythonDll))
            {
                pythonDll = "/usr/local/opt/python@3.12/Frameworks/Python.framework/Versions/3.12/lib/libpython3.12.dylib";
                pythonHome = "/usr/local/opt/python@3.12/Frameworks/Python.framework/Versions/3.12";
            }
            
            // Final fallback to a system file that always exists
            if (!File.Exists(pythonDll))
            {
                pythonDll = "/usr/lib/libc.dylib";
                pythonHome = "/usr/lib";
            }
        }
        else
        {
            pythonDll = "/lib/x86_64-linux-gnu/libc.so.6"; // Common on Linux
            pythonHome = "/lib";
        }

        return new OmniRAGOptions
        {
            PdfDirectory = "../pdf",
            ChromaPersistDirectory = "../python_env/chroma_db",
            EnableAutoIndexing = true,
            Python = new PythonOptions
            {
                DllPath = pythonDll,
                HomePath = pythonHome
            },
            LanguageModel = new LanguageModelOptions
            {
                Provider = "Phi4",
                // At least one provider must be enabled, use GPT with mock API key for testing
                GPT = new GPTOptions 
                { 
                    Enabled = true,
                    ApiKey = "test-api-key-for-validation"
                }
            },
            Resilience = new ResilienceOptions
            {
                PythonNet = new PythonNetResilienceOptions
                {
                    RetryCount = 3,
                    TimeoutSeconds = 30,
                    CircuitBreakerFailureThreshold = 3,
                    CircuitBreakerDurationSeconds = 60
                },
                VectorStore = new VectorStoreResilienceOptions
                {
                    RetryCount = 5,
                    CircuitBreakerFailureThreshold = 5,
                    CircuitBreakerDurationSeconds = 120
                },
                LanguageModel = new LanguageModelResilienceOptions
                {
                    TimeoutSeconds = 60,
                    FallbackMessage = "[LLM unavailable]"
                },
                DocumentLoader = new DocumentLoaderResilienceOptions
                {
                    EnablePerFileRecovery = true
                }
            }
        };
    }
}