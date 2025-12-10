using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OmniRAG.Core.Interfaces;

namespace OmniRAG.Infrastructure.LanguageModels;

/// <summary>
/// Factory for creating language model instances based on configuration.
/// Factory Pattern: Encapsulates object creation logic.
/// Strategy Pattern: Returns different implementations of ILanguageModel.
/// Open/Closed Principle: Easy to add new model providers.
/// </summary>
public static class LanguageModelFactory
{
    /// <summary>
    /// Creates a language model instance based on configuration settings.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="loggerFactory">Logger factory for creating loggers.</param>
    /// <returns>An initialized ILanguageModel instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid or provider is unknown.</exception>
    public static ILanguageModel Create(IConfiguration configuration, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string? provider = configuration["OmniRAG:LanguageModel:Provider"];
        
        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new InvalidOperationException(
                "Language model provider not specified in configuration. " +
                "Set 'OmniRAG:LanguageModel:Provider' to one of: Phi4, Mistral, GPT, Llama");
        }

        return provider.ToLowerInvariant() switch
        {
            "phi4" or "phi-4" => CreatePhi4Model(configuration, loggerFactory),
            "mistral" => CreateMistralModel(configuration, loggerFactory),
            "gpt" or "openai" => CreateGptModel(configuration, loggerFactory),
            "llama" => CreateLlamaModel(configuration, loggerFactory),
            _ => throw new InvalidOperationException(
                $"Unknown language model provider: '{provider}'. " +
                $"Supported providers: Phi4, Mistral, GPT, Llama")
        };
    }

    private static ILanguageModel CreatePhi4Model(IConfiguration configuration, ILoggerFactory? loggerFactory)
    {
        string? modelPath = configuration["OmniRAG:LanguageModel:Phi4:ModelPath"];
        
        if (string.IsNullOrWhiteSpace(modelPath))
        {
            throw new InvalidOperationException(
                "Phi-4 model path not specified. Set 'OmniRAG:LanguageModel:Phi4:ModelPath' in configuration.");
        }

        // Expand environment variables
        modelPath = Environment.ExpandEnvironmentVariables(modelPath);

        int maxTokens = int.TryParse(configuration["OmniRAG:LanguageModel:Phi4:MaxTokens"], out int mt) ? mt : 2048;
        float temperature = float.TryParse(configuration["OmniRAG:LanguageModel:Phi4:Temperature"], out float temp) ? temp : 0.7f;

        ILogger<Phi4LanguageModel>? logger = loggerFactory?.CreateLogger<Phi4LanguageModel>();
        
        return new Phi4LanguageModel(modelPath, maxTokens, temperature, logger);
    }

    private static ILanguageModel CreateMistralModel(IConfiguration configuration, ILoggerFactory? loggerFactory)
    {
        string? apiKey = configuration["OmniRAG:LanguageModel:Mistral:ApiKey"];
        
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Mistral API key not specified. Set 'OmniRAG:LanguageModel:Mistral:ApiKey' in configuration or environment variable.");
        }

        string modelName = configuration["OmniRAG:LanguageModel:Mistral:ModelName"] ?? "mistral-small";
        int maxTokens = int.TryParse(configuration["OmniRAG:LanguageModel:Mistral:MaxTokens"], out int mt) ? mt : 2048;
        float temperature = float.TryParse(configuration["OmniRAG:LanguageModel:Mistral:Temperature"], out float temp) ? temp : 0.7f;

        ILogger<MistralLanguageModel>? logger = loggerFactory?.CreateLogger<MistralLanguageModel>();
        
        return new MistralLanguageModel(apiKey, modelName, maxTokens, temperature, logger);
    }

    private static ILanguageModel CreateGptModel(IConfiguration configuration, ILoggerFactory? loggerFactory)
    {
        string? apiKey = configuration["OmniRAG:LanguageModel:GPT:ApiKey"];
        
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API key not specified. Set 'OmniRAG:LanguageModel:GPT:ApiKey' in configuration or environment variable.");
        }

        string modelName = configuration["OmniRAG:LanguageModel:GPT:ModelName"] ?? "gpt-4-turbo";
        string? organizationId = configuration["OmniRAG:LanguageModel:GPT:OrganizationId"];
        int maxTokens = int.TryParse(configuration["OmniRAG:LanguageModel:GPT:MaxTokens"], out int mt) ? mt : 2048;
        float temperature = float.TryParse(configuration["OmniRAG:LanguageModel:GPT:Temperature"], out float temp) ? temp : 0.7f;

        ILogger<GptLanguageModel>? logger = loggerFactory?.CreateLogger<GptLanguageModel>();
        
        return new GptLanguageModel(apiKey, modelName, organizationId, maxTokens, temperature, logger);
    }

    private static ILanguageModel CreateLlamaModel(IConfiguration configuration, ILoggerFactory? loggerFactory)
    {
        string? modelPath = configuration["OmniRAG:LanguageModel:Llama:ModelPath"];
        
        if (string.IsNullOrWhiteSpace(modelPath))
        {
            throw new InvalidOperationException(
                "Llama model path not specified. Set 'OmniRAG:LanguageModel:Llama:ModelPath' in configuration.");
        }

        // Expand environment variables
        modelPath = Environment.ExpandEnvironmentVariables(modelPath);

        string modelVariant = configuration["OmniRAG:LanguageModel:Llama:ModelVariant"] ?? "Llama-3-8B";
        int maxTokens = int.TryParse(configuration["OmniRAG:LanguageModel:Llama:MaxTokens"], out int mt) ? mt : 2048;
        float temperature = float.TryParse(configuration["OmniRAG:LanguageModel:Llama:Temperature"], out float temp) ? temp : 0.7f;

        ILogger<LlamaLanguageModel>? logger = loggerFactory?.CreateLogger<LlamaLanguageModel>();
        
        return new LlamaLanguageModel(modelPath, modelVariant, maxTokens, temperature, logger);
    }

    /// <summary>
    /// Creates a language model instance with explicit provider specification.
    /// Useful for testing or when provider is determined at runtime.
    /// </summary>
    /// <param name="provider">Language model provider name.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="loggerFactory">Logger factory for creating loggers.</param>
    /// <returns>An initialized ILanguageModel instance.</returns>
    public static ILanguageModel Create(string provider, IConfiguration configuration, ILoggerFactory? loggerFactory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentNullException.ThrowIfNull(configuration);

        return provider.ToLowerInvariant() switch
        {
            "phi4" or "phi-4" => CreatePhi4Model(configuration, loggerFactory),
            "mistral" => CreateMistralModel(configuration, loggerFactory),
            "gpt" or "openai" => CreateGptModel(configuration, loggerFactory),
            "llama" => CreateLlamaModel(configuration, loggerFactory),
            _ => throw new InvalidOperationException(
                $"Unknown language model provider: '{provider}'. " +
                $"Supported providers: Phi4, Mistral, GPT, Llama")
        };
    }
}
