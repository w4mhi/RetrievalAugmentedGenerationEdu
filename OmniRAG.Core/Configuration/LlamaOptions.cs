using System.ComponentModel.DataAnnotations;

namespace OmniRAG.Core.Configuration;

/// <summary>
/// Llama model configuration.
/// </summary>
public class LlamaOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether Llama model is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the file path to the Llama model.
    /// </summary>
    public string ModelPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Llama model variant (e.g., "Llama-3-8B", "Llama-3-70B").
    /// </summary>
    public string ModelVariant { get; set; } = "Llama-3-8B";

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
