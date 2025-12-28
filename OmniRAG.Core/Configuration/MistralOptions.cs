using System.ComponentModel.DataAnnotations;

namespace OmniRAG.Core.Configuration;

/// <summary>
/// Mistral AI configuration.
/// </summary>
public class MistralOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether Mistral AI is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the Mistral AI API key for authentication.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Mistral model name (e.g., "mistral-small", "mistral-medium").
    /// </summary>
    public string ModelName { get; set; } = "mistral-small";

    /// <summary>
    /// Gets or sets the maximum number of tokens to generate.
    /// </summary>
    [Range(1, 32768, ErrorMessage = "MaxTokens must be between 1 and 32768")]
    public int MaxTokens { get; set; } = 2048;

    /// <summary>
    /// Gets or sets the sampling temperature (0.0 = deterministic, higher = more creative).
    /// </summary>
    [Range(0.0, 2.0, ErrorMessage = "Temperature must be between 0.0 and 2.0")]
    public double Temperature { get; set; } = 0.7;
}
