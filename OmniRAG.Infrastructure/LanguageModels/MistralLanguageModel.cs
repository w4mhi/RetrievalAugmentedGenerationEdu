using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using Polly;

using OmniRAG.Core.Constants;
using OmniRAG.Core.Interfaces;
using OmniRAG.Infrastructure.Resilience;

#pragma warning disable SKEXP0010 // Suppress experimental API warnings for OpenAI connector

namespace OmniRAG.Infrastructure.LanguageModels;

/// <summary>
/// Mistral language model implementation using Semantic Kernel and Mistral API.
/// Single Responsibility: Handles Mistral model inference only.
/// Dependency Inversion: Implements ILanguageModel abstraction.
/// Mistral API is OpenAI-compatible, so we use OpenAI connector.
/// </summary>
public sealed class MistralLanguageModel : ILanguageModel, IDisposable
{
    private readonly Kernel kernel;
    private readonly IChatCompletionService chatService;
    private readonly int defaultMaxTokens;
    private readonly float defaultTemperature;
    private readonly ILogger<MistralLanguageModel>? logger;
    private readonly IAsyncPolicy<string> llmPolicy;
    private bool disposed;

    public string ModelName { get; }
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Initializes the Mistral model with Semantic Kernel.
    /// </summary>
    /// <param name="apiKey">Mistral API key.</param>
    /// <param name="modelName">Mistral model name (e.g., "mistral-small", "mistral-medium", "mistral-large").</param>
    /// <param name="maxTokens">Default maximum tokens to generate.</param>
    /// <param name="temperature">Default temperature for generation.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public MistralLanguageModel(
        string apiKey, 
        string modelName = "mistral-small",
        int maxTokens = DefaultValues.DefaultMaxTokens, 
        float temperature = DefaultValues.DefaultTemperature, 
        ILogger<MistralLanguageModel>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelName);

        this.logger = logger;
        this.defaultMaxTokens = maxTokens;
        this.defaultTemperature = temperature;
        this.ModelName = $"Mistral ({modelName})";

        this.logger?.LogDebug("Initializing Mistral model: {ModelName}, maxTokens: {MaxTokens}, temperature: {Temperature}", 
            modelName, maxTokens, temperature);

        try
        {
            // Build Semantic Kernel with OpenAI connector (Mistral is OpenAI-compatible)
            IKernelBuilder builder = Kernel.CreateBuilder();

            // Add Mistral as OpenAI-compatible chat completion
            builder.AddOpenAIChatCompletion(
                modelId: modelName,
                apiKey: apiKey,
                endpoint: new Uri("https://api.mistral.ai/v1"));

            this.kernel = builder.Build();
            this.chatService = this.kernel.GetRequiredService<IChatCompletionService>();

            // Initialize resilience policy for LLM operations (Timeout + Fallback)
            this.llmPolicy = ResiliencePolicies.CreateLanguageModelPipeline(
                this.logger,
                "[LLM unavailable] The Mistral language model is currently unavailable. Please try again later.",
                "MistralInference");

            this.IsInitialized = true;
            Console.WriteLine($"✓ Mistral model loaded: {modelName}");
            this.logger?.LogInformation("Successfully loaded Mistral model: {ModelName}", modelName);
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "Failed to initialize Mistral model {ModelName}: {ErrorMessage}", modelName, ex.Message);
            throw new InvalidOperationException(
                $"Failed to initialize Mistral model {modelName}. " +
                $"Ensure API key is valid and Mistral API is accessible.",
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

        if (!this.IsInitialized)
        {
            throw new InvalidOperationException("Model is not initialized.");
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
                    "If the context doesn't contain enough information, say so clearly. " +
                    "Always cite the source (document and page number) when possible.");

                chatHistory.AddUserMessage(prompt);

                // Configure generation settings (OpenAI-compatible for Mistral)
                OpenAIPromptExecutionSettings executionSettings = new OpenAIPromptExecutionSettings
                {
                    MaxTokens = maxTokens,
                    Temperature = temperature,
                    TopP = 0.9
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
            this.logger?.LogError(ex, "Failed to generate response from Mistral model: {ErrorMessage}", ex.Message);
            throw new InvalidOperationException(
                $"Failed to generate response from Mistral model: {ex.Message}",
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
