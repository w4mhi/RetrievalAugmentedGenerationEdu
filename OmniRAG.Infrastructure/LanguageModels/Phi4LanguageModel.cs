using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Onnx;

using Polly;

using OmniRAG.Core.Constants;
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
    public Phi4LanguageModel(string modelPath, int maxTokens = DefaultValues.DefaultMaxTokens, float temperature = DefaultValues.DefaultTemperature, ILogger<Phi4LanguageModel>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelPath);

        if (!Directory.Exists(modelPath))
        {
            throw new DirectoryNotFoundException($"Phi-4 model directory not found: {modelPath}");
        }

        this.logger = logger;
        this.defaultMaxTokens = maxTokens;
        this.defaultTemperature = temperature;
        this.ModelName = "Phi-4 Mini";

        this.logger?.LogDebug(LogMessages.InitializingLanguageModel, 
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
                FallbackMessages.LlmUnavailableRetryLater,
                "Phi4Inference");

            this.IsInitialized = true;
            Console.WriteLine($"✓ Phi-4 model loaded from: {modelPath}");
            this.logger?.LogInformation(LogMessages.LanguageModelLoaded, modelPath);
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, LogMessages.LanguageModelInitializationFailed, modelPath, ex.Message);
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

        if (!this.IsInitialized)
        {
            throw new InvalidOperationException("Model is not initialized. Call Initialize() first.");
        }

        this.logger?.LogDebug(LogMessages.GeneratingResponse, 
            prompt.Length, maxTokens, temperature);

        try
        {
            // Execute with resilience policy (Timeout + Fallback)
            string result = await this.llmPolicy.ExecuteAsync(async () =>
            {
                // Create chat history with system message and user prompt
                ChatHistory chatHistory = new ChatHistory();

                // System message for technical manual context
                chatHistory.AddSystemMessage(SystemPrompts.TechnicalAssistant);

                chatHistory.AddUserMessage(prompt);

                // Configure generation settings
                OnnxRuntimeGenAIPromptExecutionSettings executionSettings = new OnnxRuntimeGenAIPromptExecutionSettings
                {
                    MaxTokens = maxTokens,
                    Temperature = temperature,
                    TopP = DefaultValues.DefaultTopP
                };

                // Generate response
                ChatMessageContent response = await this.chatService.GetChatMessageContentAsync(
                    chatHistory,
                    executionSettings,
                    this.kernel,
                    cancellationToken);

                return response.Content ?? string.Empty;
            });

            this.logger?.LogDebug(LogMessages.ResponseGenerated, result.Length);
            return result;
        }
        catch (OperationCanceledException)
        {
            this.logger?.LogWarning(LogMessages.ResponseGenerationCancelled);
            throw;
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, LogMessages.ResponseGenerationFailed, ex.Message);
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
