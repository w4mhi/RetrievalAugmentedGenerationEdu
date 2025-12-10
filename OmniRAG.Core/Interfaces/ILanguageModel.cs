namespace OmniRAG.Core.Interfaces;

/// <summary>
/// Interface for language model implementations.
/// Strategy Pattern: Allows swapping between different LLMs (Phi-4, GPT, etc.)
/// Open/Closed Principle: Open for extension with new models, closed for modification.
/// </summary>
public interface ILanguageModel
{
    /// <summary>
    /// Generates a response based on the provided prompt.
    /// </summary>
    /// <param name="prompt">The input prompt for the language model.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The generated text response.</returns>
    Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a response with custom generation parameters.
    /// </summary>
    /// <param name="prompt">The input prompt for the language model.</param>
    /// <param name="maxTokens">Maximum number of tokens to generate.</param>
    /// <param name="temperature">Temperature for sampling (0.0 to 1.0). Higher = more creative.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The generated text response.</returns>
    Task<string> GenerateAsync(
        string prompt, 
        int maxTokens, 
        float temperature, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the name/identifier of the language model.
    /// </summary>
    string ModelName { get; }

    /// <summary>
    /// Checks if the model is initialized and ready to use.
    /// </summary>
    bool IsInitialized { get; }
}
