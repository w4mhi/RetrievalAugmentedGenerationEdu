using System.ComponentModel.DataAnnotations;

namespace OmniRAG.Core.Configuration;

/// <summary>
/// Language model configuration with provider selection.
/// </summary>
public class LanguageModelOptions
{
    [Required(ErrorMessage = "Language model provider is required")]
    public string Provider { get; set; } = "Phi4";

    public Phi4Options? Phi4 { get; set; }

    public MistralOptions? Mistral { get; set; }

    public GPTOptions? GPT { get; set; }

    public LlamaOptions? Llama { get; set; }
}
