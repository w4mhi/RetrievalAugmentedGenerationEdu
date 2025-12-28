namespace OmniRAG.Core.Constants;

/// <summary>
/// Default numeric constants for configuration values.
/// Centralizes magic numbers to improve maintainability.
/// </summary>
public static class DefaultValues
{
    // Resilience Policy Defaults
    public const int DefaultRetryCount = 3;
    public const int DefaultTimeoutSeconds = 30;
    public const int DefaultCircuitBreakerFailures = 3;
    public const int DefaultCircuitBreakerDurationSeconds = 60;
    
    // Vector Store Resilience Defaults
    public const int VectorStoreRetryCount = 5;
    public const int VectorStoreCircuitBreakerFailures = 5;
    public const int VectorStoreCircuitBreakerDurationSeconds = 120;
    
    // Language Model Defaults
    public const int LanguageModelTimeoutSeconds = 60;
    public const int DefaultMaxTokens = 2048;
    public const float DefaultTemperature = 0.7f;
    public const float DefaultTopP = 0.9f;
    public const float DefaultFrequencyPenalty = 0.0f;
    public const float DefaultPresencePenalty = 0.0f;
    
    // Embedding Defaults
    public const int DefaultMaxEmbeddingTokens = 512;
    
    // Retrieval Defaults
    public const int DefaultTopK = 5;
    public const float DefaultMinSimilarity = 0.7f;
    public const float DefaultDiversityLambda = 0.5f;
    
    // Chunking Defaults
    public const int DefaultChunkSize = 512;
    public const int DefaultOverlapSize = 100;
    
    // Command-line Option Defaults
    public const int CommandLineDefaultChunkSize = 512;
    public const int CommandLineDefaultOverlap = 100;
    public const int CommandLineDefaultTopK = 5;
    public const float CommandLineUnsetThreshold = -1.0f;
    public const int CommandLineMinTopK = 1;
    public const int CommandLineMaxTopK = 50;
    public const float CommandLineMinSimilarity = 0.0f;
    public const float CommandLineMaxSimilarity = 1.0f;
    
    // BERT Tokenizer Constants
    public const int ClsTokenId = 101;
    public const int SepTokenId = 102;
    public const int PadTokenId = 0;
    public const int MaxCharacterTokenValue = 30000;
    
    // PDF Processing Constants
    public const double HeadingFontSizeMultiplier = 1.2;
}
