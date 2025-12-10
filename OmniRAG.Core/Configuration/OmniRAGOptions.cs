using System.ComponentModel.DataAnnotations;

namespace OmniRAG.Core.Configuration;

/// <summary>
/// Root configuration options for OmniRAG application.
/// Follows Options Pattern with validation.
/// </summary>
public class OmniRAGOptions
{
    public const string Section = "OmniRAG";

    [Required(ErrorMessage = "PDF directory is required")]
    public string PdfDirectory { get; set; } = string.Empty;

    [Required(ErrorMessage = "Chroma persist directory is required")]
    public string ChromaPersistDirectory { get; set; } = string.Empty;

    public bool EnableAutoIndexing { get; set; } = true;

    [Required]
    public PythonOptions Python { get; set; } = new();

    [Required]
    public LanguageModelOptions LanguageModel { get; set; } = new();

    [Required]
    public ResilienceOptions Resilience { get; set; } = new();
}
