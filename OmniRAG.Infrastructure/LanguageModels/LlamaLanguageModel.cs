using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Onnx;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using Polly;

using OmniRAG.Core.Constants;
using OmniRAG.Core.Interfaces;
using OmniRAG.Infrastructure.Resilience;

#pragma warning disable SKEXP0010 // Suppress experimental API warnings for OpenAI connector
#pragma warning disable SKEXP0070 // Suppress experimental API warnings for ONNX connector

namespace OmniRAG.Infrastructure.LanguageModels;

/// <summary>
/// Llama language model implementation using Semantic Kernel and ONNX runtime.
/// Single Responsibility: Handles Llama model inference only (local execution).
/// Dependency Inversion: Implements ILanguageModel abstraction.
/// Supports Llama 2, Llama 3, and other Llama variants in ONNX format.
/// </summary>
public sealed class LlamaLanguageModel : ILanguageModel, IDisposable
{
    private readonly Kernel kernel;
    private readonly IChatCompletionService chatService;
    private readonly int defaultMaxTokens;
    private readonly float defaultTemperature;
    private readonly ILogger<LlamaLanguageModel>? logger;
    private readonly IAsyncPolicy<string> llmPolicy;
    private bool disposed;

    public string ModelName { get; }
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Initializes the Llama model with Semantic Kernel.
    /// </summary>
    /// <param name="modelPath">Path to the Llama ONNX model directory.</param>
    /// <param name="modelVariant">Llama model variant (e.g., "Llama-2-7B", "Llama-3-8B").</param>
    /// <param name="maxTokens">Default maximum tokens to generate.</param>
    /// <param name="temperature">Default temperature for generation.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public LlamaLanguageModel(
        string modelPath, 
        string modelVariant = "Llama-3",
        int maxTokens = DefaultValues.DefaultMaxTokens, 
        float temperature = DefaultValues.DefaultTemperature, 
        ILogger<LlamaLanguageModel>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelVariant);

        if (!Directory.Exists(modelPath))
        {
            throw new DirectoryNotFoundException($"Llama model directory not found: {modelPath}");
        }

        this.logger = logger;
        this.defaultMaxTokens = maxTokens;
        this.defaultTemperature = temperature;
        this.ModelName = $"Llama ({modelVariant})";

        this.logger?.LogDebug("Initializing Llama model from path: {ModelPath}, variant: {Variant}, maxTokens: {MaxTokens}, temperature: {Temperature}", 
            modelPath, modelVariant, maxTokens, temperature);

        try
        {
            // Build Semantic Kernel with ONNX connector
            IKernelBuilder builder = Kernel.CreateBuilder();

            // Add Llama ONNX model
            builder.AddOnnxRuntimeGenAIChatCompletion(
                modelId: modelVariant,
                modelPath: modelPath);

            this.kernel = builder.Build();
            this.chatService = this.kernel.GetRequiredService<IChatCompletionService>();

            // Initialize resilience policy for LLM operations (Timeout + Fallback)
            this.llmPolicy = ResiliencePolicies.CreateLanguageModelPipeline(
                this.logger,
                "[LLM unavailable] The Llama language model is currently unavailable. Please try again later.",
                "LlamaInference");

            this.IsInitialized = true;
            Console.WriteLine($"✓ Llama model loaded from: {modelPath} ({modelVariant})");
            this.logger?.LogInformation("Successfully loaded Llama model from: {ModelPath}, variant: {Variant}", modelPath, modelVariant);
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "Failed to initialize Llama model from {ModelPath}: {ErrorMessage}", modelPath, ex.Message);
            throw new InvalidOperationException(
                $"Failed to initialize Llama model from {modelPath}. " +
                $"Ensure the ONNX model files are present and ONNX runtime is available.",
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
                // Note: Some Llama variants may handle system messages differently
                chatHistory.AddSystemMessage(
                    "You are a helpful technical assistant specialized in answering questions about technical manuals and user guides. " +
                    "Provide accurate, concise answers based on the provided context. " +
                    "If the context doesn't contain enough information, say so clearly. " +
                    "Always cite the source (document and page number) when possible.");

                chatHistory.AddUserMessage(prompt);

                // Configure generation settings for ONNX runtime
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
            this.logger?.LogError(ex, "Failed to generate response from Llama model: {ErrorMessage}", ex.Message);
            throw new InvalidOperationException(
                $"Failed to generate response from Llama model: {ex.Message}",
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
