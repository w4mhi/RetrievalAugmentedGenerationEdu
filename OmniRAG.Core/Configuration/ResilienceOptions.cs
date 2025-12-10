using System.ComponentModel.DataAnnotations;

namespace OmniRAG.Core.Configuration;

/// <summary>
/// Resilience and fault tolerance configuration.
/// </summary>
public class ResilienceOptions
{
    [Required]
    public PythonNetResilienceOptions PythonNet { get; set; } = new();

    [Required]
    public VectorStoreResilienceOptions VectorStore { get; set; } = new();

    [Required]
    public LanguageModelResilienceOptions LanguageModel { get; set; } = new();

    [Required]
    public DocumentLoaderResilienceOptions DocumentLoader { get; set; } = new();
}
