using System.ComponentModel.DataAnnotations;

namespace OmniRAG.Core.Configuration;

/// <summary>
/// Language model configuration with provider selection.
/// </summary>
public class LanguageModelOptions
{
    /// <summary>
    /// Gets or sets the language model provider name (e.g., "Phi4", "GPT", "Mistral", "Llama").
    /// </summary>
    [Required(ErrorMessage = "Language model provider is required")]
    public string Provider { get; set; } = "Phi4";

    /// <summary>
    /// Gets or sets the Phi-4 model configuration.
    /// </summary>
    public Phi4Options? Phi4 { get; set; }

    /// <summary>
    /// Gets or sets the Mistral AI configuration.
    /// </summary>
    public MistralOptions? Mistral { get; set; }

    /// <summary>
    /// Gets or sets the OpenAI GPT configuration.
    /// </summary>
    public GPTOptions? GPT { get; set; }

    /// <summary>
    /// Gets or sets the Llama model configuration.
    /// </summary>
    public LlamaOptions? Llama { get; set; }
}
