using System.ComponentModel.DataAnnotations;

namespace OmniRAG.Core.Configuration;

/// <summary>
/// OpenAI GPT configuration.
/// </summary>
public class GPTOptions
{
    public bool Enabled { get; set; }

    public string ApiKey { get; set; } = string.Empty;

    public string OrganizationId { get; set; } = string.Empty;

    public string ModelName { get; set; } = "gpt-4-turbo";

    [Range(1, 128000, ErrorMessage = "MaxTokens must be between 1 and 128000")]
    public int MaxTokens { get; set; } = 2048;

    [Range(0.0, 2.0, ErrorMessage = "Temperature must be between 0.0 and 2.0")]
    public double Temperature { get; set; } = 0.7;
}
