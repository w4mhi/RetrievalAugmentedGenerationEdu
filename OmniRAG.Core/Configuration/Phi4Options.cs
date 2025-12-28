using System.ComponentModel.DataAnnotations;

namespace OmniRAG.Core.Configuration;

/// <summary>
/// Phi-4 model configuration.
/// </summary>
public class Phi4Options
{
    public bool Enabled { get; set; }

    public string ModelPath { get; set; } = string.Empty;

    [Range(1, 32768, ErrorMessage = "MaxTokens must be between 1 and 32768")]
    public int MaxTokens { get; set; } = 2048;

    [Range(0.0, 2.0, ErrorMessage = "Temperature must be between 0.0 and 2.0")]
    public double Temperature { get; set; } = 0.7;

    [Range(0.0, 1.0, ErrorMessage = "TopP must be between 0.0 and 1.0")]
    public double TopP { get; set; } = 0.9;

    [Range(-2.0, 2.0, ErrorMessage = "FrequencyPenalty must be between -2.0 and 2.0")]
    public double FrequencyPenalty { get; set; } = 0.0;

    [Range(-2.0, 2.0, ErrorMessage = "PresencePenalty must be between -2.0 and 2.0")]
    public double PresencePenalty { get; set; } = 0.0;
}
