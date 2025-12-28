using System.ComponentModel.DataAnnotations;

namespace OmniRAG.Core.Configuration;

/// <summary>
/// Root configuration options for OmniRAG application.
/// Follows Options Pattern with validation.
/// </summary>
public class OmniRAGOptions
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public const string Section = "OmniRAG";

    /// <summary>
    /// Gets or sets the directory path containing PDF documents to process.
    /// </summary>
    [Required(ErrorMessage = "PDF directory is required")]
    public string PdfDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the directory path where Chroma vector store persists data.
    /// </summary>
    [Required(ErrorMessage = "Chroma persist directory is required")]
    public string ChromaPersistDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether automatic document indexing is enabled.
    /// </summary>
    public bool EnableAutoIndexing { get; set; } = true;

    /// <summary>
    /// Gets or sets the Python runtime configuration.
    /// </summary>
    [Required]
    public PythonOptions Python { get; set; } = new();

    /// <summary>
    /// Gets or sets the language model configuration.
    /// </summary>
    [Required]
    public LanguageModelOptions LanguageModel { get; set; } = new();

    /// <summary>
    /// Gets or sets the resilience and fault tolerance configuration.
    /// </summary>
    [Required]
    public ResilienceOptions Resilience { get; set; } = new();
}
