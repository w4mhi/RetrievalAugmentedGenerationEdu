namespace OmniRAG.Core.Constants;

/// <summary>
/// Centralized log message templates for structured logging.
/// Prevents magic strings throughout the codebase.
/// </summary>
public static class LogMessages
{
    // RagEngine messages
    public const string RagEngineInitialized = "RagEngine initialized with {RetrievalStrategy} retrieval strategy, TopK={TopK}, LLM={LlmEnabled}";
    public const string ProcessingQuery = "Processing query: {Query}";
    public const string GeneratingEmbedding = "Generating embedding for query";
    public const string EmbeddingGenerated = "Generated embedding with {Dimensions} dimensions";
    public const string SearchingVectorStore = "Searching vector store with {Strategy} strategy, TopK={TopK}";
    public const string RetrievedResults = "Retrieved {ResultCount} results for query in {ElapsedMs}ms";
    public const string GeneratingAnswerWithLlm = "Generating answer using LLM";
    public const string LlmNotConfigured = "LLM not configured, using fallback retrieval mode";
    public const string QueryProcessedSuccessfully = "Query processed successfully in {TotalSeconds}s";
    public const string QueryProcessingFailed = "Failed to process query after {ElapsedMs}ms: {ErrorMessage}";
    public const string DirectoryNotFound = "Directory not found: {DirectoryPath}";
    public const string StartingDocumentIndexing = "Starting document indexing from: {DirectoryPath}";
    public const string LoadingDocuments = "Loading documents from directory";
    public const string LoadedChunks = "Loaded {ChunkCount} chunks from documents";
    public const string NoDocumentsFound = "No documents found in directory: {DirectoryPath}";
    public const string StoringChunks = "Storing chunks in vector store";
    public const string IndexedSuccessfully = "Successfully indexed {ChunkCount} chunks in {ElapsedSeconds}s";
    public const string IndexingFailed = "Failed to index documents after {ElapsedMs}ms: {ErrorMessage}";
    public const string RetrievingIndexStats = "Retrieving index statistics";
    public const string IndexStatsRetrieved = "Index contains {ChunkCount} chunks, last indexed: {LastIndexed}";
    public const string IndexStatsRetrievalFailed = "Failed to retrieve index statistics: {ErrorMessage}";
    public const string NoSearchResults = "No search results available for LLM generation";
    public const string SendingPromptToLlm = "Sending prompt to LLM with {ContextLength} characters";
    public const string LlmGeneratedAnswer = "LLM generated answer with {AnswerLength} characters";
    public const string LlmGenerationFailed = "LLM generation failed: {ErrorMessage}";
    public const string FallingBackToRetrieval = "Falling back to retrieval-only mode due to LLM error";
    public const string ResponseGenerationCancelled = "Response generation was cancelled";
    
    // Resilience messages
    public const string CircuitBreakerOpened = "{OperationName}: Circuit breaker opened for {DurationSeconds}s due to {ExceptionType}";
    public const string CircuitBreakerReset = "{OperationName}: Circuit breaker reset, operations resuming";
    public const string CircuitBreakerHalfOpen = "{OperationName}: Circuit breaker half-open, testing recovery";
    public const string RetryAttempt = "{OperationName}: Retry {RetryCount}/{MaxRetries} after {DelaySeconds}s: {ErrorMessage}";
    public const string OperationTimedOut = "{OperationName}: Operation timed out after {TimeoutSeconds}s";
    public const string LlmInferenceTimedOut = "{OperationName}: LLM inference timed out after {TimeoutSeconds}s";
    public const string LlmOperationFailed = "{OperationName}: LLM operation failed, using fallback: {ErrorMessage}";
    public const string VectorStoreCircuitBreakerOpened = "{OperationName}: Vector store circuit breaker opened for {DurationSeconds}s (possible disk/DB issues)";
    public const string VectorStoreCircuitBreakerReset = "{OperationName}: Vector store circuit breaker reset";
    public const string VectorStoreRetry = "{OperationName}: Vector store retry {RetryCount}/{MaxRetries} after {DelayMs}ms: {ErrorMessage}";
    public const string DocumentLoadingFailed = "{OperationName}: Failed to process file {FilePath}, skipping: {ErrorMessage}";
    
    // Vector Store messages
    public const string InitializingVectorStore = "Initializing ChromaVectorStore with persist directory: {PersistDirectory}";
    public const string VectorStoreReady = "ChromaDB collection ready: {CollectionName} at {PersistDirectory}";
    public const string VectorStoreInitializationFailed = "Failed to initialize ChromaVectorStore: {ErrorMessage}";
    public const string StoringChunksInVectorStore = "Storing {ChunkCount} chunks in vector store";
    public const string StoredChunksSuccessfully = "Successfully stored {ChunkCount} chunks in vector store";
    public const string ErrorStoringChunks = "Error storing chunks in vector store: {ErrorMessage}";
    public const string SearchingVectorStoreWithStrategy = "Searching vector store with strategy: {Strategy}, topK: {TopK}";
    public const string SearchCompleted = "Search completed, returning {ResultCount} results";
    public const string ErrorSearchingVectorStore = "Error searching vector store: {ErrorMessage}";
    
    // Language Model messages
    public const string InitializingLanguageModel = "Initializing Phi-4 model from path: {ModelPath}, maxTokens: {MaxTokens}, temperature: {Temperature}";
    public const string LanguageModelLoaded = "Successfully loaded Phi-4 model from: {ModelPath}";
    public const string LanguageModelInitializationFailed = "Failed to initialize Phi-4 model from {ModelPath}: {ErrorMessage}";
    public const string GeneratingResponse = "Generating response for prompt of length {PromptLength}, maxTokens: {MaxTokens}, temperature: {Temperature}";
    public const string ResponseGenerated = "Successfully generated response of length: {ResponseLength}";
    public const string ResponseGenerationFailed = "Failed to generate response from Phi-4 model: {ErrorMessage}";
}
