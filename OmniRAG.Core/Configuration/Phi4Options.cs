using System.ComponentModel.DataAnnotations;

namespace OmniRAG.Core.Configuration;

/// <summary>
/// Phi-4 model configuration.
/// </summary>
public class Phi4Options
{
    /// <summary>
    /// Gets or sets a value indicating whether Phi-4 model is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the file path to the Phi-4 model.
    /// </summary>
    public string ModelPath { get; set; } = string.Empty;

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

    /// <summary>
    /// Gets or sets the nucleus sampling probability (top-p sampling).
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "TopP must be between 0.0 and 1.0")]
    public double TopP { get; set; } = 0.9;

    /// <summary>
    /// Gets or sets the frequency penalty to reduce repetition of token sequences.
    /// </summary>
    [Range(-2.0, 2.0, ErrorMessage = "FrequencyPenalty must be between -2.0 and 2.0")]
    public double FrequencyPenalty { get; set; } = 0.0;

    /// <summary>
    /// Gets or sets the presence penalty to encourage topic diversity.
    /// </summary>
    [Range(-2.0, 2.0, ErrorMessage = "PresencePenalty must be between -2.0 and 2.0")]
    public double PresencePenalty { get; set; } = 0.0;
}
