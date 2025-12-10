# OmniRAG - Architecture Diagram

## System Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                         User Interface                          │
│                    (OmniRAG.Console)                        │
│                                                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │   Program    │  │ OmniRAG  │  │   Spectre    │           │
│  │   (DI Setup) │──│     App      │──│   Console    │           │
│  └──────────────┘  └──────────────┘  └──────────────┘           │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│                      Core Business Logic                        │
│                      (OmniRAG.Core)                         │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                      IRagEngine                          │   │
│  │  ┌────────────────────────────────────────────────────┐  │   │
│  │  │             RagEngine Service                      │  │   │
│  │  │  - IndexDocumentsAsync()                           │  │   │
│  │  │  - QueryAsync()                                    │  │   │
│  │  │  - GetIndexStatsAsync()                            │  │   │
│  │  └────────────────────────────────────────────────────┘  │   │
│  └──────────────────────────────────────────────────────────┘   │
│                         │                                       │
│                         ↓                                       │
│  ┌─────────────────┬─────────────────┬──────────────────┐       │
│  │ IDocumentLoader │ IEmbeddingService│ ILanguageModel  │       │
│  │                 │                  │  (NEW)          │       │
│  │  Models:        │                  │                 │       │
│  │  - DocumentChunk│                  │                 │       │
│  │  - SearchResult │                  │                 │       │
│  │  - RagResponse  │                  │                 │       │
│  └─────────────────┴─────────────────┴──────────────────┘       │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│                     Infrastructure Layer                        │
│                 (OmniRAG.Infrastructure)                    │
│                                                                 │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────┐   │
│  │ PdfDocumentLoader│  │SentenceTransform │  │ ChromaVector │   │
│  │                  │  │ EmbeddingService │  │    Store     │   │
│  │  Uses:           │  │                  │  │              │   │
│  │  - PdfTextExtract│  │  Uses:           │  │  Uses:       │   │
│  │  - SemanticChunk │  │  - Python.NET    │  │  - ChromaDB  │   │
│  └──────────────────┘  │  - sentence-     │  │  - Python.NET│   │
│                        │    transformers  │  │              │   │
│                        └──────────────────┘  └──────────────┘   │
│                                                                 │
│  ┌────────────────────────────────────────────────────────────┐│
│  │           Language Models (ILanguageModel)                 ││
│  │                                                            ││
│  │  ┌────────────┐  ┌────────────┐  ┌──────┐  ┌──────────┐  ││
│  │  │Phi4Language│  │MistralLang │  │GPT   │  │Llama     │  ││
│  │  │Model       │  │uageModel   │  │Lang  │  │Language  │  ││
│  │  │(ONNX)      │  │(API)       │  │Model │  │Model     │  ││
│  │  └────────────┘  └────────────┘  └──────┘  └──────────┘  ││
│  │                                                            ││
│  │           ┌──────────────────────────┐                    ││
│  │           │ LanguageModelFactory     │                    ││
│  │           │ (Creates models from     │                    ││
│  │           │  appsettings.json)       │                    ││
│  │           └──────────────────────────┘                    ││
│  └────────────────────────────────────────────────────────────┘│
│                                                                 │
│  ┌────────────────────────────────────────────────────────────┐│
│  │              Resilience Policies (Polly 8.4)               ││
│  │  - Circuit Breaker - Retry - Timeout - Fallback            ││
│  └────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│                      External Dependencies                      │
│                                                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │  UglyToad    │  │   Python     │  │    Sentence          │   │
│  │   PdfPig     │  │   Runtime    │  │  Transformers        │   │
│  │              │  │              │  │  (5 embedding models)│   │
│  └──────────────┘  └──────────────┘  └──────────────────────┘   │
│                                                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │   ChromaDB   │  │  Mistral API │  │   OpenAI API         │   │
│  │  (Vector DB) │  │              │  │   (GPT-4 Turbo)      │   │
│  └──────────────┘  └──────────────┘  └──────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

---

## Language Model Architecture

OmniRAG supports 4 language model providers through a **Strategy Pattern** implementation. All models are interchangeable via the `ILanguageModel` interface.

### Strategy Pattern Design

```
┌─────────────────────────────────────────────────┐
│           ILanguageModel Interface              │
│  (Strategy Pattern - All models interchangeable)│
│                                                 │
│  + Task<string> GenerateAsync(string prompt)   │
│  + string ModelName { get; }                   │
│  + string ModelType { get; }                   │
└──────────────┬──────────────────────────────────┘
               │
       ┌───────┴────────┬──────────┬──────────┐
       │                │          │          │
┌──────▼─────┐  ┌──────▼─────┐  ┌─▼────┐  ┌──▼────┐
│Phi4Language│  │MistralLang │  │GPT   │  │Llama  │
│   Model    │  │uageModel   │  │Lang  │  │Language│
│            │  │            │  │Model │  │Model  │
│  Type:     │  │  Type:     │  │Type: │  │Type:  │
│  ONNX      │  │  Cloud API │  │Cloud │  │ONNX   │
│            │  │            │  │API   │  │       │
│  Uses:     │  │  Uses:     │  │Uses: │  │Uses:  │
│  - Semantic│  │  - Semantic│  │-Sem  │  │-Sem   │
│    Kernel  │  │    Kernel  │  │ Kern │  │ Kern  │
│  - ONNX    │  │  - OpenAI  │  │-Open │  │-ONNX  │
│    Runtime │  │    Connector│  │ AI  │  │ Run   │
│    GenAI   │  │            │  │Conn  │  │       │
└────────────┘  └────────────┘  └──────┘  └───────┘
```

### Factory Pattern Implementation

```
         ┌──────────────────────────────────┐
         │   LanguageModelFactory           │
         │   (Static Factory Class)         │
         │                                  │
         │  + Create(config, logger)        │
         │  + Create(provider, config, ...)│
         └────────┬─────────────────────────┘
                  │
      ┌───────────┼───────────┬──────────┐
      │           │           │          │
 ┌────▼────┐ ┌───▼────┐ ┌───▼────┐ ┌───▼────┐
 │CreatePhi│ │CreateMi│ │CreateGp│ │CreateLl│
 │4Model() │ │stralMo │ │tModel()│ │amaModel│
 └─────────┘ │del()   │ └────────┘ └────────┘
             └────────┘
             
   Reads appsettings.json:
   {
     "LanguageModel": {
       "Provider": "Phi4"  // or "Mistral", "GPT", "Llama"
     }
   }
```

### Model Comparison

| Model | Type | Connector | Execution Settings | Best For |
|-------|------|-----------|-------------------|----------|
| **Phi-4** | Local ONNX | OnnxRuntimeGenAIChatCompletion | OnnxRuntimeGenAIPromptExecutionSettings | Privacy-focused, free |
| **Llama** | Local ONNX | OnnxRuntimeGenAIChatCompletion | OnnxRuntimeGenAIPromptExecutionSettings | Open-source, customizable |
| **Mistral** | Cloud API | OpenAIChatCompletion | OpenAIPromptExecutionSettings | Cost-effective, fast |
| **GPT** | Cloud API | OpenAIChatCompletion | OpenAIPromptExecutionSettings | Best quality |

### Resilience Integration

All language models include production-grade resilience patterns:

```
┌─────────────────────────────────────────┐
│      Language Model (any provider)      │
└──────────────┬──────────────────────────┘
               │
               ↓
┌─────────────────────────────────────────┐
│         Resilience Pipeline             │
│  (ResiliencePolicies.CreateLanguage     │
│   ModelPipeline)                        │
│                                         │
│  1. Timeout (60s default)               │
│     └─ Prevents hanging requests        │
│                                         │
│  2. Fallback                            │
│     └─ Returns user-friendly message    │
│        on timeout/failure               │
└─────────────────────────────────────────┘
```

### Configuration-Driven Model Selection

1. **Development**: Use local models (free, privacy-focused)
```json
{
  "LanguageModel": {
    "Provider": "Phi4"
  }
}
```

2. **Production**: Use cloud models (fast, scalable)
```json
{
  "LanguageModel": {
    "Provider": "GPT",
    "GPT": {
      "ApiKey": "your-api-key",
      "ModelName": "gpt-4-turbo"
    }
  }
}
```

3. **Privacy-Sensitive**: Always use local models
```json
{
  "LanguageModel": {
    "Provider": "Llama",
    "Llama": {
      "ModelPath": "C:\\Models\\Llama-3-8B-ONNX"
    }
  }
}
```

### Key Design Decisions

**Why Strategy Pattern?**
- ✅ Models are completely interchangeable
- ✅ Add new models without modifying existing code (Open/Closed Principle)
- ✅ Each model handles only its specific provider (Single Responsibility)

**Why Factory Pattern?**
- ✅ Centralized model creation logic
- ✅ Configuration-driven instantiation
- ✅ Easier testing (can mock factory)
- ✅ Environment variable expansion

**Why Semantic Kernel?**
- ✅ Unified API for ONNX and cloud models
- ✅ Built-in connector ecosystem
- ✅ Microsoft-supported framework
- ✅ Integration with future features (agents, planners)

---

## System Overview (Original)

```
┌─────────────────────────────────────────────────────────────────┐
│                         User Interface                          │
│                    (OmniRAG.Console)                        │
│                                                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │   Program    │  │ OmniRAG  │  │   Spectre    │           │
│  │   (DI Setup) │──│     App      │──│   Console    │           │
│  └──────────────┘  └──────────────┘  └──────────────┘           │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│                      Core Business Logic                        │
│                      (OmniRAG.Core)                         │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                      IRagEngine                          │   │
│  │  ┌────────────────────────────────────────────────────┐  │   │
│  │  │             RagEngine Service                      │  │   │
│  │  │  - IndexDocumentsAsync()                           │  │   │
│  │  │  - QueryAsync()                                    │  │   │
│  │  │  - GetIndexStatsAsync()                            │  │   │
│  │  └────────────────────────────────────────────────────┘  │   │
│  └──────────────────────────────────────────────────────────┘   │
│                         │                                       │
│                         ↓                                       │
│  ┌─────────────────┬─────────────────┬──────────────────┐       │
│  │ IDocumentLoader │ IEmbeddingService│ IVectorStore    │       │
│  │                 │                  │                 │       │
│  │  Models:        │                  │                 │       │
│  │  - DocumentChunk│                  │                 │       │
│  │  - SearchResult │                  │                 │       │
│  │  - RagResponse  │                  │                 │       │
│  └─────────────────┴─────────────────┴──────────────────┘       │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│                     Infrastructure Layer                        │
│                 (OmniRAG.Infrastructure)                    │
│                                                                 │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────┐   │
│  │ PdfDocumentLoader│  │SentenceTransform │  │ ChromaVector │   │
│  │                  │  │ EmbeddingService │  │    Store     │   │
│  │  Uses:           │  │                  │  │              │   │
│  │  - PdfTextExtract│  │  Uses:           │  │  Uses:       │   │
│  │  - SemanticChunk │  │  - Python.NET    │  │  - ChromaDB  │   │
│  └──────────────────┘  │  - sentence-     │  │  - Python.NET│   │
│                        │    transformers  │  │              │   │
│                        └──────────────────┘  └──────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│                      External Dependencies                      │
│                                                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │  UglyToad    │  │   Python     │  │    Sentence          │   │
│  │   PdfPig     │  │   Runtime    │  │  Transformers        │   │
│  │              │  │              │  │  (all-MiniLM-L6-v2)  │   │
│  └──────────────┘  └──────────────┘  └──────────────────────┘   │
│                                                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │   ChromaDB   │  │    Spectre   │  │   Semantic Kernel    │   │
│  │   (Vector    │  │   Console    │  │   (Future: Phi-4)    │   │
│  │    Store)    │  │              │  │                      │   │
│  └──────────────┘  └──────────────┘  └──────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

## Data Flow

```
┌─────────────┐
│  User Query │
└──────┬──────┘
       │
       ↓
┌──────────────────────────────────┐
│ 1. Generate Query Embedding      │
│    (SentenceTransformer)         │
└──────┬───────────────────────────┘
       │
       ↓
┌──────────────────────────────────┐
│ 2. Search Vector Store           │
│    (ChromaDB L2 Distance)        │
│    Returns: Top 5 chunks         │
└──────┬───────────────────────────┘
       │
       ↓
┌──────────────────────────────────┐
│ 3. Build Context from Chunks     │
│    Include: source, page, section│
└──────┬───────────────────────────┘
       │
       ↓
┌──────────────────────────────────┐
│ 4. Generate Answer               │
│    (Currently: Simple concat)    │
│    (Future: Phi-4 LLM)           │
└──────┬───────────────────────────┘
       │
       ↓
┌──────────────────────────────────┐
│ 5. Return RagResponse            │
│    - Answer text                 │
│    - Source citations            │
│    - Processing time             │
└──────────────────────────────────┘
```

## Indexing Flow

```
┌─────────────┐
│ PDF Files   │
│ in ./pdf/   │
└──────┬──────┘
       │
       ↓
┌──────────────────────────────────┐
│ 1. Extract Text & Metadata       │
│    (PdfPig)                      │
│    - Text per page               │
│    - Headings (font analysis)    │
└──────┬───────────────────────────┘
       │
       ↓
┌──────────────────────────────────┐
│ 2. Semantic Chunking             │
│    - 512 tokens per chunk        │
│    - 100 token overlap           │
│    - Section-aware               │
└──────┬───────────────────────────┘
       │
       ↓
┌──────────────────────────────────┐
│ 3. Generate Embeddings           │
│    (sentence-transformers)       │
│    384-dim vectors               │
└──────┬───────────────────────────┘
       │
       ↓
┌──────────────────────────────────┐
│ 4. Store in Vector DB            │
│    (ChromaDB)                    │
│    - Embeddings                  │
│    - Metadata                    │
│    - Original text               │
└──────────────────────────────────┘
```

## Component Dependencies

```
OmniRAG.Console
    │
    ├── OmniRAG.Core (Domain)
    │   └── Models (POCO)
    │   └── Interfaces (Contracts)
    │   └── Services (Business Logic)
    │
    └── OmniRAG.Infrastructure (Implementations)
        ├── Depends on: OmniRAG.Core
        ├── PdfProcessing/
        │   ├── PdfTextExtractor (UglyToad.PdfPig)
        │   └── SemanticChunker (Custom)
        ├── DocumentLoaders/
        │   └── PdfDocumentLoader
        ├── Embeddings/
        │   └── SentenceTransformerEmbeddingService (Python.NET)
        └── VectorStores/
            └── ChromaVectorStore (Python.NET → ChromaDB)
```

## Technology Stack

| Layer          | Technology              | Purpose                    |
|----------------|------------------------|----------------------------|
| UI             | Spectre.Console        | Beautiful terminal UI      |
| Framework      | .NET 9.0               | Modern C# features         |
| DI Container   | Microsoft.Extensions   | Dependency injection       |
| PDF Processing | UglyToad.PdfPig       | Extract text from PDFs     |
| Embeddings     | sentence-transformers  | Generate text embeddings   |
| Vector DB      | ChromaDB              | Similarity search          |
| Python Interop | pythonnet             | Python integration         |
| Testing        | xUnit + Moq           | Unit testing               |
| Future LLM     | Phi-4 (AI Toolkit)    | Answer generation          |

## Performance Characteristics

| Operation          | Time        | Notes                      |
|-------------------|-------------|----------------------------|
| PDF Loading       | 50ms/page   | Depends on PDF complexity  |
| Chunking          | 1ms/page    | CPU-bound                  |
| Embedding (CPU)   | 70μs/chunk  | Batch processing           |
| Vector Store      | 10ms/insert | ChromaDB local             |
| Search            | 30ms        | L2 distance, top-5         |
| Total Query       | 1-3s        | Without LLM generation     |

