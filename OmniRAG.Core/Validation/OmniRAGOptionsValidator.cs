using Microsoft.Extensions.Options;
using OmniRAG.Core.Configuration;

namespace OmniRAG.Core.Validation;

/// <summary>
/// Custom validator for OmniRAGOptions with file system and runtime checks.
/// Implements IValidateOptions for startup validation.
/// </summary>
public class OmniRAGOptionsValidator : IValidateOptions<OmniRAGOptions>
{
    public ValidateOptionsResult Validate(string? name, OmniRAGOptions options)
    {
        List<string> failures = new List<string>();

        // Validate Python configuration
        ValidatePythonConfiguration(options.Python, failures);

        // Validate directories
        ValidateDirectories(options, failures);

        // Validate language model configuration
        ValidateLanguageModelConfiguration(options.LanguageModel, failures);

        // Validate resilience configuration
        ValidateResilienceConfiguration(options.Resilience, failures);

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static void ValidatePythonConfiguration(PythonOptions python, List<string> failures)
    {
        // Expand environment variables
        string dllPath = Environment.ExpandEnvironmentVariables(python.DllPath);
        string homePath = Environment.ExpandEnvironmentVariables(python.HomePath);

        // Validate Python DLL exists
        if (!File.Exists(dllPath))
        {
            failures.Add($"Python DLL not found at: {dllPath}");
            failures.Add("  → Install Python 3.12 or update 'OmniRAG:Python:DllPath' in appsettings.json");
        }

        // Validate Python home directory exists
        if (!Directory.Exists(homePath))
        {
            failures.Add($"Python home directory not found at: {homePath}");
            failures.Add("  → Install Python 3.12 or update 'OmniRAG:Python:HomePath' in appsettings.json");
        }
    }

    private static void ValidateDirectories(OmniRAGOptions options, List<string> failures)
    {
        // PDF directory validation (warning only, will be created if needed)
        string pdfDirectory = Environment.ExpandEnvironmentVariables(options.PdfDirectory);
        if (!string.IsNullOrEmpty(pdfDirectory) && !Directory.Exists(pdfDirectory))
        {
            try
            {
                Directory.CreateDirectory(pdfDirectory);
            }
            catch (Exception ex)
            {
                failures.Add($"Cannot create PDF directory at: {pdfDirectory}");
                failures.Add($"  → Error: {ex.Message}");
            }
        }

        // Chroma persist directory validation (will be created by ChromaDB if needed)
        string chromaDirectory = Environment.ExpandEnvironmentVariables(options.ChromaPersistDirectory);
        if (!string.IsNullOrEmpty(chromaDirectory))
        {
            try
            {
                string? parentDir = Path.GetDirectoryName(chromaDirectory);
                if (parentDir != null && !Directory.Exists(parentDir))
                {
                    Directory.CreateDirectory(parentDir);
                }
            }
            catch (Exception ex)
            {
                failures.Add($"Cannot create parent directory for Chroma DB at: {chromaDirectory}");
                failures.Add($"  → Error: {ex.Message}");
            }
        }
    }

    private static void ValidateLanguageModelConfiguration(LanguageModelOptions languageModel, List<string> failures)
    {
        bool hasEnabledProvider = false;

        // Validate Phi-4 configuration
        if (languageModel.Phi4?.Enabled == true)
        {
            hasEnabledProvider = true;
            ValidatePhi4Configuration(languageModel.Phi4, failures);
        }

        // Validate Mistral configuration
        if (languageModel.Mistral?.Enabled == true)
        {
            hasEnabledProvider = true;
            ValidateMistralConfiguration(languageModel.Mistral, failures);
        }

        // Validate GPT configuration
        if (languageModel.GPT?.Enabled == true)
        {
            hasEnabledProvider = true;
            ValidateGPTConfiguration(languageModel.GPT, failures);
        }

        // Validate Llama configuration
        if (languageModel.Llama?.Enabled == true)
        {
            hasEnabledProvider = true;
            ValidateLlamaConfiguration(languageModel.Llama, failures);
        }

        // At least one provider must be enabled
        if (!hasEnabledProvider)
        {
            failures.Add("No language model provider is enabled");
            failures.Add("  → Enable at least one provider in 'OmniRAG:LanguageModel' section");
        }
    }

    private static void ValidatePhi4Configuration(Phi4Options phi4, List<string> failures)
    {
        string modelPath = Environment.ExpandEnvironmentVariables(phi4.ModelPath);

        if (string.IsNullOrWhiteSpace(modelPath))
        {
            failures.Add("Phi-4 is enabled but ModelPath is not configured");
            failures.Add("  → Set 'OmniRAG:LanguageModel:Phi4:ModelPath' in appsettings.json");
            return;
        }

        if (!Directory.Exists(modelPath))
        {
            failures.Add($"Phi-4 model directory not found at: {modelPath}");
            failures.Add("  → Download Phi-4 model or update path in appsettings.json");
        }
        else
        {
            // Check for required model files
            string[] requiredFiles = { "phi-4.onnx", "phi-4.onnx.data", "genai_config.json" };
            List<string> missingFiles = new List<string>();

            foreach (string file in requiredFiles)
            {
                string filePath = Path.Combine(modelPath, file);
                if (!File.Exists(filePath))
                {
                    missingFiles.Add(file);
                }
            }

            if (missingFiles.Count > 0)
            {
                failures.Add($"Phi-4 model directory is missing required files: {string.Join(", ", missingFiles)}");
                failures.Add("  → Ensure complete Phi-4 model is downloaded");
            }
        }
    }

    private static void ValidateMistralConfiguration(MistralOptions mistral, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(mistral.ApiKey))
        {
            failures.Add("Mistral is enabled but ApiKey is not configured");
            failures.Add("  → Set 'OmniRAG:LanguageModel:Mistral:ApiKey' in appsettings.json or environment variables");
        }
    }

    private static void ValidateGPTConfiguration(GPTOptions gpt, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(gpt.ApiKey))
        {
            failures.Add("GPT is enabled but ApiKey is not configured");
            failures.Add("  → Set 'OmniRAG:LanguageModel:GPT:ApiKey' in appsettings.json or environment variables");
        }
    }

    private static void ValidateLlamaConfiguration(LlamaOptions llama, List<string> failures)
    {
        string modelPath = Environment.ExpandEnvironmentVariables(llama.ModelPath);

        if (string.IsNullOrWhiteSpace(modelPath))
        {
            failures.Add("Llama is enabled but ModelPath is not configured");
            failures.Add("  → Set 'OmniRAG:LanguageModel:Llama:ModelPath' in appsettings.json");
            return;
        }

        if (!File.Exists(modelPath) && !Directory.Exists(modelPath))
        {
            failures.Add($"Llama model not found at: {modelPath}");
            failures.Add("  → Download Llama model or update path in appsettings.json");
        }
    }

    private static void ValidateResilienceConfiguration(ResilienceOptions resilience, List<string> failures)
    {
        // Resilience configuration is mostly validated by data annotations
        // Additional custom validation can be added here if needed

        if (resilience.PythonNet.RetryCount > resilience.PythonNet.CircuitBreakerFailureThreshold)
        {
            failures.Add("PythonNet RetryCount should not exceed CircuitBreakerFailureThreshold");
            failures.Add("  → Adjust 'OmniRAG:Resilience:PythonNet' configuration");
        }

        if (resilience.VectorStore.RetryCount > resilience.VectorStore.CircuitBreakerFailureThreshold)
        {
            failures.Add("VectorStore RetryCount should not exceed CircuitBreakerFailureThreshold");
            failures.Add("  → Adjust 'OmniRAG:Resilience:VectorStore' configuration");
        }
    }
}
