using System.ComponentModel.DataAnnotations;

namespace OmniRAG.Core.Configuration;

/// <summary>
/// Language model resilience configuration.
/// </summary>
public class LanguageModelResilienceOptions
{
    [Range(1, 600, ErrorMessage = "TimeoutSeconds must be between 1 and 600")]
    public int TimeoutSeconds { get; set; } = 60;

    [Required(ErrorMessage = "FallbackMessage is required")]
    public string FallbackMessage { get; set; } = "[LLM unavailable] The language model is currently unavailable.";
}
