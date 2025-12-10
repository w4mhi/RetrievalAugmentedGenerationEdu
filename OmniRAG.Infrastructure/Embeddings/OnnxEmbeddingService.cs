using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Polly;
using OmniRAG.Core.Interfaces;
using OmniRAG.Infrastructure.Resilience;
using System.Collections.Concurrent;

namespace OmniRAG.Infrastructure.Embeddings;

/// <summary>
/// Pure .NET ONNX embedding service - eliminates Python.NET dependency.
/// Uses Microsoft.ML.OnnxRuntime for direct ONNX model inference.
/// Strategy Pattern: Configurable with different ONNX embedding models.
/// Dependency Inversion Principle: Depends on IEmbeddingService abstraction.
/// </summary>
public sealed class OnnxEmbeddingService : IEmbeddingService, IDisposable
{
    private readonly InferenceSession session;
    private readonly BertTokenizer tokenizer;
    private readonly int dimensions;
    private readonly int maxTokens;
    private readonly string modelName;
    private readonly ILogger<OnnxEmbeddingService>? logger;
    private readonly IAsyncPolicy<float[]> embeddingPolicy;
    private readonly IAsyncPolicy<IReadOnlyList<float[]>> batchEmbeddingPolicy;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance with the specified ONNX model and tokenizer.
    /// </summary>
    /// <param name="modelPath">Path to ONNX model file (e.g., model.onnx).</param>
    /// <param name="tokenizerPath">Path to tokenizer model (e.g., tokenizer.json).</param>
    /// <param name="modelName">Human-readable model name for logging.</param>
    /// <param name="dimensions">Expected embedding dimensions.</param>
    /// <param name="maxTokens">Maximum tokens to process (default: 512).</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public OnnxEmbeddingService(
        string modelPath,
        string tokenizerPath,
        string modelName,
        int dimensions,
        int maxTokens = 512,
        ILogger<OnnxEmbeddingService>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelPath, nameof(modelPath));
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenizerPath, nameof(tokenizerPath));
        ArgumentException.ThrowIfNullOrWhiteSpace(modelName, nameof(modelName));

        if (dimensions <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dimensions), "Dimensions must be positive.");
        }

        if (maxTokens <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxTokens), "Max tokens must be positive.");
        }

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"ONNX model not found: {modelPath}");
        }

        if (!File.Exists(tokenizerPath))
        {
            throw new FileNotFoundException($"Tokenizer not found: {tokenizerPath}");
        }

        this.modelName = modelName;
        this.dimensions = dimensions;
        this.maxTokens = maxTokens;
        this.logger = logger;

        // Initialize resilience policies
        this.embeddingPolicy = ResiliencePolicies.CreatePythonNetPipeline<float[]>(
            this.logger,
            "OnnxEmbeddingGeneration");

        this.batchEmbeddingPolicy = ResiliencePolicies.CreatePythonNetPipeline<IReadOnlyList<float[]>>(
            this.logger,
            "OnnxBatchEmbeddingGeneration");

        this.logger?.LogInformation(
            "Initializing ONNX embedding service: {ModelName}, dimensions: {Dimensions}, max tokens: {MaxTokens}",
            modelName, dimensions, maxTokens);

        try
        {
            // Initialize ONNX Runtime session with optimizations
            SessionOptions sessionOptions = new SessionOptions
            {
                EnableCpuMemArena = true,
                EnableMemoryPattern = true,
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                ExecutionMode = ExecutionMode.ORT_SEQUENTIAL
            };

            this.session = new InferenceSession(modelPath, sessionOptions);

            // Load BERT tokenizer
            // For sentence-transformers models, we use a simplified BERT tokenizer
            this.tokenizer = new BertTokenizer(tokenizerPath, maxTokens);

            this.logger?.LogInformation(
                "Successfully loaded ONNX model: {ModelName} ({Dimensions} dimensions)",
                modelName, dimensions);
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "Failed to initialize ONNX embedding service: {ErrorMessage}", ex.Message);
            Dispose();
            throw;
        }
    }

    /// <summary>
    /// Gets the name of the loaded model.
    /// </summary>
    public string ModelName => this.modelName;

    /// <summary>
    /// Gets the embedding dimensions.
    /// </summary>
    public int Dimensions => this.dimensions;

    /// <summary>
    /// Gets the maximum number of tokens processed.
    /// </summary>
    public int MaxTokens => this.maxTokens;

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text, nameof(text));
        ObjectDisposedException.ThrowIf(this.disposed, this);

        this.logger?.LogDebug("Generating embedding for text of length: {TextLength}", text.Length);

        try
        {
            // Execute with resilience policy (Circuit Breaker + Retry + Timeout)
            float[] result = await this.embeddingPolicy.ExecuteAsync(async () =>
            {
                return await Task.Run(() => this.GenerateEmbeddingCore(text), cancellationToken);
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

        ObjectDisposedException.ThrowIf(this.disposed, this);

        this.logger?.LogDebug("Generating embeddings for batch of {Count} texts", textList.Count);

        try
        {
            // Execute with resilience policy (Circuit Breaker + Retry + Timeout)
            IReadOnlyList<float[]> results = await this.batchEmbeddingPolicy.ExecuteAsync(async () =>
            {
                return await Task.Run(() =>
                {
                    ConcurrentBag<(int Index, float[] Embedding)> embeddings = new ConcurrentBag<(int Index, float[] Embedding)>();

                    // Process in parallel for better performance
                    Parallel.For(0, textList.Count, new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount,
                        CancellationToken = cancellationToken
                    },
                    i =>
                    {
                        float[] embedding = this.GenerateEmbeddingCore(textList[i]);
                        embeddings.Add((i, embedding));
                    });

                    // Sort by original index to maintain order
                    return embeddings
                        .OrderBy(x => x.Index)
                        .Select(x => x.Embedding)
                        .ToList();
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
    }

    /// <summary>
    /// Core embedding generation logic (synchronous for ONNX Runtime).
    /// </summary>
    private float[] GenerateEmbeddingCore(string text)
    {
        // Tokenize text
        (long[] tokens, long[] attentionMask, long[] tokenTypeIds) = this.tokenizer.Tokenize(text);

        // Create input tensors
        DenseTensor<long> inputIds = new DenseTensor<long>(tokens, new[] { 1, this.maxTokens });
        DenseTensor<long> attentionMaskTensor = new DenseTensor<long>(attentionMask, new[] { 1, this.maxTokens });
        DenseTensor<long> tokenTypeIdsTensor = new DenseTensor<long>(tokenTypeIds, new[] { 1, this.maxTokens });

        // Prepare inputs for ONNX model (BERT models require all 3 inputs)
        List<NamedOnnxValue> inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor),
            NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIdsTensor)
        };

        // Run inference
        using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = this.session.Run(inputs);
        Tensor<float> outputTensor = results.First().AsTensor<float>();

        // Extract embedding (usually from last hidden state, mean pooling)
        int actualLength = attentionMask.Count(x => x == 1);
        return this.MeanPooling(outputTensor, attentionMask, actualLength);
    }

    /// <summary>
    /// Mean pooling over token embeddings (weighted by attention mask).
    /// </summary>
    private float[] MeanPooling(Tensor<float> tokenEmbeddings, long[] attentionMask, int actualLength)
    {
        float[] embedding = new float[this.dimensions];
        int validTokenCount = 0;

        // Sum embeddings for valid tokens (where attention mask = 1)
        for (int i = 0; i < actualLength; i++)
        {
            if (attentionMask[i] == 1)
            {
                for (int j = 0; j < this.dimensions; j++)
                {
                    // tokenEmbeddings is [batch_size, sequence_length, hidden_size]
                    // We're using batch_size=1, so access [0, i, j]
                    embedding[j] += tokenEmbeddings[0, i, j];
                }
                validTokenCount++;
            }
        }

        // Average (mean pooling)
        if (validTokenCount > 0)
        {
            for (int j = 0; j < this.dimensions; j++)
            {
                embedding[j] /= validTokenCount;
            }
        }

        // L2 normalization (common for sentence embeddings)
        return NormalizeL2(embedding);
    }

    /// <summary>
    /// L2 normalization (unit vector).
    /// </summary>
    private static float[] NormalizeL2(float[] vector)
    {
        double sumSquares = 0;
        for (int i = 0; i < vector.Length; i++)
        {
            sumSquares += vector[i] * vector[i];
        }

        float magnitude = (float)Math.Sqrt(sumSquares);
        if (magnitude > 0)
        {
            for (int i = 0; i < vector.Length; i++)
            {
                vector[i] /= magnitude;
            }
        }

        return vector;
    }

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.session?.Dispose();
        this.disposed = true;

        this.logger?.LogDebug("ONNX embedding service disposed: {ModelName}", this.modelName);
    }
}

/// <summary>
/// Simplified BERT tokenizer for sentence-transformers models.
/// This is a basic implementation - for production, consider using a full tokenizer library.
/// </summary>
internal class BertTokenizer
{
    private readonly int maxTokens;
    private const int ClsTokenId = 101;  // [CLS] token
    private const int SepTokenId = 102;  // [SEP] token
    private const int PadTokenId = 0;    // [PAD] token

    public BertTokenizer(string tokenizerPath, int maxTokens)
    {
        this.maxTokens = maxTokens;
        // Note: For full implementation, load vocabulary from tokenizer.json
        // This is a simplified version for demonstration
    }

    public (long[] tokens, long[] attentionMask, long[] tokenTypeIds) Tokenize(string text)
    {
        // Simplified tokenization - in production, use proper BPE/WordPiece tokenization
        // For now, use character-level tokenization as placeholder
        long[] tokens = new long[this.maxTokens];
        long[] attentionMask = new long[this.maxTokens];
        long[] tokenTypeIds = new long[this.maxTokens]; // All zeros for single sentence

        // Add [CLS] token
        tokens[0] = ClsTokenId;
        attentionMask[0] = 1;
        tokenTypeIds[0] = 0;

        // Simple character encoding (placeholder - use proper tokenizer in production)
        int position = 1;
        foreach (char c in text)
        {
            if (position >= this.maxTokens - 1)
            {
                break;
            }

            tokens[position] = (long)Math.Clamp((int)c, 0, 30000); // Simple mapping
            attentionMask[position] = 1;
            tokenTypeIds[position] = 0; // First sentence segment
            position++;
        }

        // Add [SEP] token
        if (position < this.maxTokens)
        {
            tokens[position] = SepTokenId;
            attentionMask[position] = 1;
            tokenTypeIds[position] = 0;
            position++;
        }

        // Pad remaining positions
        for (int i = position; i < this.maxTokens; i++)
        {
            tokens[i] = PadTokenId;
            attentionMask[i] = 0;
            tokenTypeIds[i] = 0;
        }

        return (tokens, attentionMask, tokenTypeIds);
    }
}
