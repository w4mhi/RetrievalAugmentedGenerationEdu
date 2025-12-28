using System.ComponentModel.DataAnnotations;

namespace OmniRAG.Core.Configuration;

/// <summary>
/// OpenAI GPT configuration.
/// </summary>
public class GPTOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether OpenAI GPT is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the OpenAI API key for authentication.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OpenAI organization ID (optional).
    /// </summary>
    public string OrganizationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the GPT model name (e.g., "gpt-4-turbo", "gpt-3.5-turbo").
    /// </summary>
    public string ModelName { get; set; } = "gpt-4-turbo";

    /// <summary>
    /// Gets or sets the maximum number of tokens to generate.
    /// </summary>
    [Range(1, 128000, ErrorMessage = "MaxTokens must be between 1 and 128000")]
    public int MaxTokens { get; set; } = 2048;

    /// <summary>
    /// Gets or sets the sampling temperature (0.0 = deterministic, higher = more creative).
    /// </summary>
    [Range(0.0, 2.0, ErrorMessage = "Temperature must be between 0.0 and 2.0")]
    public double Temperature { get; set; } = 0.7;
}
