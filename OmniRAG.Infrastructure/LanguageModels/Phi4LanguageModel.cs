using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Onnx;
using Polly;
using OmniRAG.Core.Interfaces;
using OmniRAG.Infrastructure.Resilience;

#pragma warning disable SKEXP0070 // Suppress experimental API warnings for ONNX connector

namespace OmniRAG.Infrastructure.LanguageModels;

/// <summary>
/// Phi-4 language model implementation using Semantic Kernel and ONNX runtime.
/// Single Responsibility: Handles Phi-4 model inference only.
/// Dependency Inversion: Implements ILanguageModel abstraction.
/// </summary>
public sealed class Phi4LanguageModel : ILanguageModel, IDisposable
{
    private readonly Kernel kernel;
    private readonly IChatCompletionService chatService;
    private readonly int defaultMaxTokens;
    private readonly float defaultTemperature;
    private readonly ILogger<Phi4LanguageModel>? logger;
    private readonly IAsyncPolicy<string> llmPolicy;
    private bool disposed;

    public string ModelName { get; }
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Initializes the Phi-4 model with Semantic Kernel.
    /// </summary>
    /// <param name="modelPath">Path to the Phi-4 ONNX model directory.</param>
    /// <param name="maxTokens">Default maximum tokens to generate.</param>
    /// <param name="temperature">Default temperature for generation.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public Phi4LanguageModel(string modelPath, int maxTokens = 2048, float temperature = 0.7f, ILogger<Phi4LanguageModel>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelPath);

        if (!Directory.Exists(modelPath))
        {
            throw new DirectoryNotFoundException($"Phi-4 model directory not found: {modelPath}");
        }

        this.defaultMaxTokens = maxTokens;
        this.defaultTemperature = temperature;
        this.logger = logger;
        this.ModelName = "Phi-4 Mini";

        this.logger?.LogDebug("Initializing Phi-4 model from path: {ModelPath}, maxTokens: {MaxTokens}, temperature: {Temperature}", 
            modelPath, maxTokens, temperature);

        try
        {
            // Build Semantic Kernel with ONNX connector
            IKernelBuilder builder = Kernel.CreateBuilder();

            // Add Phi-4 ONNX model
            builder.AddOnnxRuntimeGenAIChatCompletion(
                modelId: "phi-4",
                modelPath: modelPath);

            this.kernel = builder.Build();
            this.chatService = this.kernel.GetRequiredService<IChatCompletionService>();

            // Initialize resilience policy for LLM operations (Timeout + Fallback)
            this.llmPolicy = ResiliencePolicies.CreateLanguageModelPipeline(
                this.logger,
                "[LLM unavailable] The language model is currently unavailable. Please try again later.",
                "Phi4Inference");

            IsInitialized = true;
            Console.WriteLine($"✓ Phi-4 model loaded from: {modelPath}");
            this.logger?.LogInformation("Successfully loaded Phi-4 model from: {ModelPath}", modelPath);
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "Failed to initialize Phi-4 model from {ModelPath}: {ErrorMessage}", modelPath, ex.Message);
            throw new InvalidOperationException(
                $"Failed to initialize Phi-4 model from {modelPath}. " +
                $"Ensure AI Toolkit has downloaded the model and ONNX runtime is available.",
                ex);
        }
    }

    public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        return await GenerateAsync(prompt, this.defaultMaxTokens, this.defaultTemperature, cancellationToken);
    }

    public async Task<string> GenerateAsync(
        string prompt,
        int maxTokens,
        float temperature,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        if (!IsInitialized)
        {
            throw new InvalidOperationException("Model is not initialized. Call Initialize() first.");
        }

        this.logger?.LogDebug("Generating response for prompt of length {PromptLength}, maxTokens: {MaxTokens}, temperature: {Temperature}", 
            prompt.Length, maxTokens, temperature);

        try
        {
            // Execute with resilience policy (Timeout + Fallback)
            string result = await this.llmPolicy.ExecuteAsync(async () =>
            {
                // Create chat history with system message and user prompt
                ChatHistory chatHistory = new ChatHistory();

                // System message for technical manual context
                chatHistory.AddSystemMessage(
                    "You are a helpful technical assistant specialized in answering questions about technical manuals and user guides. " +
                    "Provide accurate, concise answers based on the provided context. " +
                    "If the context doesn''t contain enough information, say so clearly. " +
                    "Always cite the source (document and page number) when possible.");

                chatHistory.AddUserMessage(prompt);

                // Configure generation settings
                OnnxRuntimeGenAIPromptExecutionSettings executionSettings = new OnnxRuntimeGenAIPromptExecutionSettings
                {
                    MaxTokens = maxTokens,
                    Temperature = temperature,
                    TopP = 0.9f
                };

                // Generate response
                ChatMessageContent response = await this.chatService.GetChatMessageContentAsync(
                    chatHistory,
                    executionSettings,
                    this.kernel,
                    cancellationToken);

                return response.Content ?? string.Empty;
            });

            this.logger?.LogDebug("Successfully generated response of length: {ResponseLength}", result.Length);
            return result;
        }
        catch (OperationCanceledException)
        {
            this.logger?.LogWarning("Response generation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "Failed to generate response from Phi-4 model: {ErrorMessage}", ex.Message);
            throw new InvalidOperationException(
                $"Failed to generate response from Phi-4 model: {ex.Message}",
                ex);
        }
    }

    public void Dispose()
    {
        if (this.disposed) return;

        // Semantic Kernel handles disposal of services
        this.disposed = true;
    }
}
