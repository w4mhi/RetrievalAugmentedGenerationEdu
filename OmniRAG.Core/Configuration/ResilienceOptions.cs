using System.ComponentModel.DataAnnotations;

namespace OmniRAG.Core.Configuration;

/// <summary>
/// Resilience and fault tolerance configuration.
/// </summary>
public class ResilienceOptions
{
    /// <summary>
    /// Gets or sets the Python.NET resilience configuration.
    /// </summary>
    [Required]
    public PythonNetResilienceOptions PythonNet { get; set; } = new();

    /// <summary>
    /// Gets or sets the vector store resilience configuration.
    /// </summary>
    [Required]
    public VectorStoreResilienceOptions VectorStore { get; set; } = new();

    /// <summary>
    /// Gets or sets the language model resilience configuration.
    /// </summary>
    [Required]
    public LanguageModelResilienceOptions LanguageModel { get; set; } = new();

    /// <summary>
    /// Gets or sets the document loader resilience configuration.
    /// </summary>
    [Required]
    public DocumentLoaderResilienceOptions DocumentLoader { get; set; } = new();
}
