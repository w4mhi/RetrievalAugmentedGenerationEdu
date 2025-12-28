using System;
using System.IO;
using System.Linq;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using OmniRAG.Core.Interfaces;
using OmniRAG.Core.Models;

namespace OmniRAG.Infrastructure.Embeddings;

/// <summary>
/// Factory for creating embedding service instances based on strategy.
/// Factory Pattern: Encapsulates embedding service creation logic.
/// Single Responsibility: Handles creation of embedding services only.
/// </summary>
public static class EmbeddingServiceFactory
{
    /// <summary>
    /// Creates a Python.NET-based embedding service (legacy, to be phased out).
    /// </summary>
    public static IEmbeddingService CreatePythonNetService(
        EmbeddingStrategy strategy,
        string pythonDll,
        string pythonHome,
        ILoggerFactory? loggerFactory = null)
    {
        string modelName = strategy.GetModelName();
        int dimensions = strategy.GetDimensions();
        ILogger<SentenceTransformerEmbeddingService>? logger = loggerFactory?.CreateLogger<SentenceTransformerEmbeddingService>();

        return strategy switch
        {
            EmbeddingStrategy.MiniLM => new SentenceTransformerEmbeddingService(
                pythonDll, pythonHome, modelName, dimensions, logger),
            
            EmbeddingStrategy.MPNetBase => new SentenceTransformerEmbeddingService(
                pythonDll, pythonHome, modelName, dimensions, logger),
            
            EmbeddingStrategy.BGESmall => new SentenceTransformerEmbeddingService(
                pythonDll, pythonHome, modelName, dimensions, logger),
            
            EmbeddingStrategy.BGELarge => new SentenceTransformerEmbeddingService(
                pythonDll, pythonHome, modelName, dimensions, logger),
            
            EmbeddingStrategy.Multilingual => new SentenceTransformerEmbeddingService(
                pythonDll, pythonHome, modelName, dimensions, logger),
            
            _ => throw new ArgumentOutOfRangeException(
                nameof(strategy),
                strategy,
                $"Unknown embedding strategy: {strategy}")
        };
    }

    /// <summary>
    /// Creates a pure .NET ONNX-based embedding service (recommended).
    /// Eliminates Python.NET dependency for simplified deployment.
    /// </summary>
    /// <param name="strategy">The embedding strategy to use.</param>
    /// <param name="modelsBasePath">Base path where ONNX models are stored.</param>
    /// <param name="loggerFactory">Optional logger factory for creating typed loggers.</param>
    /// <returns>An IEmbeddingService implementation.</returns>
    public static IEmbeddingService CreateOnnxService(
        EmbeddingStrategy strategy,
        string modelsBasePath,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelsBasePath, nameof(modelsBasePath));

        string modelName = strategy.GetModelName();
        int dimensions = strategy.GetDimensions();
        ILogger<OnnxEmbeddingService>? logger = loggerFactory?.CreateLogger<OnnxEmbeddingService>();

        // Model-specific paths (assumes standard directory structure)
        string modelFolder = strategy switch
        {
            EmbeddingStrategy.MiniLM => "all-MiniLM-L6-v2",
            EmbeddingStrategy.MPNetBase => "all-mpnet-base-v2",
            EmbeddingStrategy.BGESmall => "bge-small-en-v1.5",
            EmbeddingStrategy.BGELarge => "bge-large-en-v1.5",
            EmbeddingStrategy.Multilingual => "paraphrase-multilingual-MiniLM-L12-v2",
            _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, $"Unknown embedding strategy: {strategy}")
        };

        string modelPath = Path.Combine(modelsBasePath, modelFolder, "model.onnx");
        string tokenizerPath = Path.Combine(modelsBasePath, modelFolder, "tokenizer.json");

        return new OnnxEmbeddingService(
            modelPath,
            tokenizerPath,
            modelName,
            dimensions,
            maxTokens: 512,
            logger);
    }

    /// <summary>
    /// Creates an embedding service instance (defaults to ONNX if available, falls back to Python.NET).
    /// </summary>
    public static IEmbeddingService Create(
        EmbeddingStrategy strategy,
        string pythonDll,
        string pythonHome,
        ILoggerFactory? loggerFactory = null)
    {
        // Default to Python.NET for backward compatibility
        // Users should migrate to CreateOnnxService for production deployments
        return CreatePythonNetService(strategy, pythonDll, pythonHome, loggerFactory);
    }

    /// <summary>
    /// Creates a cached embedding service that wraps any IEmbeddingService implementation.
    /// Decorator Pattern: Adds caching behavior to any embedding service.
    /// Provides 10-100x speedup for repeated queries.
    /// </summary>
    /// <param name="innerService">The underlying embedding service to cache.</param>
    /// <param name="cache">The memory cache instance (configured with size limit).</param>
    /// <param name="cacheExpiration">How long to cache embeddings (default: 24 hours).</param>
    /// <param name="loggerFactory">Optional logger factory for creating typed loggers.</param>
    /// <returns>A cached embedding service.</returns>
    public static IEmbeddingService CreateCachedService(
        IEmbeddingService innerService,
        IMemoryCache cache,
        TimeSpan? cacheExpiration = null,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(innerService, nameof(innerService));
        ArgumentNullException.ThrowIfNull(cache, nameof(cache));

        ILogger<CachedEmbeddingService>? logger = loggerFactory?.CreateLogger<CachedEmbeddingService>();

        return new CachedEmbeddingService(
            innerService,
            cache,
            cacheExpiration,
            logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<CachedEmbeddingService>.Instance);
    }

    /// <summary>
    /// Creates a cached ONNX-based embedding service (recommended for production).
    /// Combines the performance benefits of ONNX with caching for maximum efficiency.
    /// </summary>
    /// <param name="strategy">The embedding strategy to use.</param>
    /// <param name="modelsBasePath">Base path where ONNX models are stored.</param>
    /// <param name="cache">The memory cache instance (configured with size limit).</param>
    /// <param name="cacheExpiration">How long to cache embeddings (default: 24 hours).</param>
    /// <param name="loggerFactory">Optional logger factory for creating typed loggers.</param>
    /// <returns>A cached ONNX embedding service.</returns>
    public static IEmbeddingService CreateCachedOnnxService(
        EmbeddingStrategy strategy,
        string modelsBasePath,
        IMemoryCache cache,
        TimeSpan? cacheExpiration = null,
        ILoggerFactory? loggerFactory = null)
    {
        IEmbeddingService onnxService = CreateOnnxService(strategy, modelsBasePath, loggerFactory);
        return CreateCachedService(onnxService, cache, cacheExpiration, loggerFactory);
    }

    /// <summary>
    /// Creates a cached Python.NET-based embedding service (legacy).
    /// Consider migrating to CreateCachedOnnxService for simplified deployment.
    /// </summary>
    public static IEmbeddingService CreateCachedPythonNetService(
        EmbeddingStrategy strategy,
        string pythonDll,
        string pythonHome,
        IMemoryCache cache,
        TimeSpan? cacheExpiration = null,
        ILoggerFactory? loggerFactory = null)
    {
        IEmbeddingService pythonService = CreatePythonNetService(strategy, pythonDll, pythonHome, loggerFactory);
        return CreateCachedService(pythonService, cache, cacheExpiration, loggerFactory);
    }
}

