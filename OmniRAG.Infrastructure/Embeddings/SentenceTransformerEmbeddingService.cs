using Microsoft.Extensions.Logging;
using Polly;
using OmniRAG.Core.Interfaces;
using OmniRAG.Infrastructure.Resilience;
using Python.Runtime;

namespace OmniRAG.Infrastructure.Embeddings;

/// <summary>
/// Embedding service using sentence-transformers via Python.NET.
/// Strategy Pattern: Configurable with different embedding models.
/// Dependency Inversion Principle: Depends on IEmbeddingService abstraction.
/// </summary>
public sealed class SentenceTransformerEmbeddingService : IEmbeddingService, IDisposable
{
    private readonly dynamic model;
    private readonly IntPtr threadState;
    private readonly string modelName;
    private readonly int dimensions;
    private readonly ILogger<SentenceTransformerEmbeddingService>? logger;
    private readonly IAsyncPolicy<float[]> embeddingPolicy;
    private readonly IAsyncPolicy<IReadOnlyList<float[]>> batchEmbeddingPolicy;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance with the specified model.
    /// </summary>
    /// <param name="pythonDll">Path to Python DLL.</param>
    /// <param name="pythonHome">Python home directory path.</param>
    /// <param name="modelName">Hugging Face model identifier.</param>
    /// <param name="dimensions">Expected embedding dimensions.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public SentenceTransformerEmbeddingService(
        string pythonDll,
        string pythonHome,
        string modelName,
        int dimensions,
        ILogger<SentenceTransformerEmbeddingService>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pythonDll, nameof(pythonDll));
        ArgumentException.ThrowIfNullOrWhiteSpace(pythonHome, nameof(pythonHome));
        ArgumentException.ThrowIfNullOrWhiteSpace(modelName, nameof(modelName));

        if (dimensions <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dimensions), "Dimensions must be positive.");
        }

        this.modelName = modelName;
        this.dimensions = dimensions;
        this.logger = logger;

        // Initialize resilience policies for Python.NET operations
        this.embeddingPolicy = ResiliencePolicies.CreatePythonNetPipeline<float[]>(
            this.logger,
            "EmbeddingGeneration");
        
        this.batchEmbeddingPolicy = ResiliencePolicies.CreatePythonNetPipeline<IReadOnlyList<float[]>>(
            this.logger,
            "BatchEmbeddingGeneration");

        this.logger?.LogDebug("Initializing SentenceTransformer with model: {ModelName}, dimensions: {Dimensions}", modelName, dimensions);

        try
        {
            // Initialize Python runtime
            Runtime.PythonDLL = pythonDll;
            PythonEngine.PythonHome = pythonHome;
            PythonEngine.Initialize();
            threadState = PythonEngine.BeginAllowThreads();

            // Import and load model
            using (Py.GIL())
            {
                dynamic sentenceTransformers = Py.Import("sentence_transformers");
                model = sentenceTransformers.SentenceTransformer(this.modelName);
                System.Console.WriteLine($"Loaded embedding model: {this.modelName} ({this.dimensions} dimensions)");
                this.logger?.LogInformation("Successfully loaded embedding model: {ModelName} ({Dimensions} dimensions)", this.modelName, this.dimensions);
            }
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "Failed to initialize SentenceTransformer: {ErrorMessage}", ex.Message);
            throw;
        }
    }    /// <summary>
    /// Gets the name of the loaded model.
    /// </summary>
    public string ModelName => modelName;

    /// <summary>
    /// Gets the embedding dimensions.
    /// </summary>
    public int Dimensions => dimensions;

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text, nameof(text));
        this.logger?.LogDebug("Generating embedding for text of length: {TextLength}", text.Length);

        try
        {
            // Execute with resilience policy (Circuit Breaker + Retry + Timeout)
            float[] result = await this.embeddingPolicy.ExecuteAsync(async () =>
            {
                return await Task.Run(() =>
                {
                    using (Py.GIL())
                    {
                        dynamic embedding = model.encode(text);
                        return ConvertToFloatArray(embedding);
                    }
                }, cancellationToken);
            });

            this.logger?.LogDebug("Successfully generated embedding with {Dimensions} dimensions", result.Length);
            return result;
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "Error generating embedding: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        List<string> textList = texts.ToList();

        if (textList.Count == 0)
        {
            this.logger?.LogWarning("Empty text list provided for batch embedding generation");
            return Array.Empty<float[]>();
        }

        this.logger?.LogDebug("Generating embeddings for batch of {Count} texts", textList.Count);

        try
        {
            // Execute with resilience policy (Circuit Breaker + Retry + Timeout)
            IReadOnlyList<float[]> results = await this.batchEmbeddingPolicy.ExecuteAsync(async () =>
            {
                return await Task.Run(() =>
                {
                    using (Py.GIL())
                    {
                        PyList pyList = new PyList();
                        foreach (string text in textList)
                        {
                            pyList.Append(new PyString(text));
                        }

                        dynamic embeddings = model.encode(pyList);

                        List<float[]> embeddingList = new List<float[]>();
                        for (int i = 0; i < textList.Count; i++)
                        {
                            dynamic embedding = embeddings[i];
                            embeddingList.Add(ConvertToFloatArray(embedding));
                        }

                        return embeddingList;
                    }
                }, cancellationToken);
            });

            this.logger?.LogDebug("Successfully generated {Count} embeddings", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "Error generating batch embeddings: {ErrorMessage}", ex.Message);
            throw;
        }
    }    private static float[] ConvertToFloatArray(dynamic numpyArray)
    {
        dynamic tolist = numpyArray.tolist();
        PyList pyList = new PyList(tolist);
        float[] result = new float[pyList.Length()];
        
        for (int i = 0; i < pyList.Length(); i++)
        {
            result[i] = (float)pyList[i].As<double>();
        }
        
        return result;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        PythonEngine.EndAllowThreads(threadState);
        PythonEngine.Shutdown();
        disposed = true;
    }
}
