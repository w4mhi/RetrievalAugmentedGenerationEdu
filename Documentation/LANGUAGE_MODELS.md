# OmniRAG Language Models Guide

## Overview
OmniRAG supports multiple language model providers through a clean **Strategy Pattern** implementation. All models implement the `ILanguageModel` interface, making them interchangeable via configuration.

---

## Supported Language Models

| Model | Type | Cost | Privacy | Speed | Setup Complexity |
|-------|------|------|---------|-------|------------------|
| **Phi-4** | Local ONNX | Free | 100% Local | Medium | Easy |
| **Llama** | Local ONNX | Free | 100% Local | Medium | Easy |
| **Mistral** | API | Pay-per-token | Cloud | Fast | Very Easy |
| **GPT** | API | Pay-per-token | Cloud | Fast | Very Easy |

---

## Model Details

### 1. Phi-4 (Microsoft) 🔷
**Best for**: Local, privacy-sensitive deployments

**Characteristics**:
- ✅ **Local execution** (no internet required)
- ✅ **Free** (no API costs)
- ✅ **Privacy** (data never leaves your machine)
- ✅ **Small model** (~7GB download via AI Toolkit)
- ⚠️ **Slower** inference (CPU/GPU dependent)
- ⚠️ **Requires** AI Toolkit or manual ONNX setup

**Configuration**:
```json
{
  "OmniRAG": {
    "LanguageModel": {
      "Provider": "Phi4",
      "Phi4": {
        "ModelPath": "%USERPROFILE%\\.aitk\\models\\phi-4",
        "MaxTokens": 2048,
        "Temperature": 0.7
      }
    }
  }
}
```

**Setup Instructions**:
1. Install Visual Studio Code AI Toolkit extension
2. Download Phi-4 model through AI Toolkit
3. Set `ModelPath` to downloaded location
4. Run OmniRAG

**Code Example**:
```csharp
var logger = loggerFactory.CreateLogger<Phi4LanguageModel>();
var model = new Phi4LanguageModel(
    modelPath: @"C:\Users\YourName\.aitk\models\phi-4",
    maxTokens: 2048,
    temperature: 0.7f,
    logger: logger);

string response = await model.GenerateAsync("What is RAG?");
```

---

### 2. Llama (Meta) 🦙
**Best for**: Local, open-source deployments with larger models

**Characteristics**:
- ✅ **Local execution** (no internet required)
- ✅ **Free** (no API costs)
- ✅ **Privacy** (data never leaves your machine)
- ✅ **Multiple sizes** (7B, 13B, 70B variants)
- ✅ **Open source** (fully customizable)
- ⚠️ **Larger models** require significant VRAM/RAM
- ⚠️ **ONNX conversion** may be required

**Configuration**:
```json
{
  "OmniRAG": {
    "LanguageModel": {
      "Provider": "Llama",
      "Llama": {
        "ModelPath": "C:\\Models\\Llama-3-8B-ONNX",
        "ModelVariant": "Llama-3-8B",
        "MaxTokens": 2048,
        "Temperature": 0.7
      }
    }
  }
}
```

**Setup Instructions**:
1. Download Llama model from Hugging Face (e.g., Llama-3-8B)
2. Convert to ONNX format (or download ONNX version)
3. Set `ModelPath` to ONNX model directory
4. Specify `ModelVariant` for identification
5. Run OmniRAG

**Code Example**:
```csharp
var logger = loggerFactory.CreateLogger<LlamaLanguageModel>();
var model = new LlamaLanguageModel(
    modelPath: @"C:\Models\Llama-3-8B-ONNX",
    modelVariant: "Llama-3-8B",
    maxTokens: 2048,
    temperature: 0.7f,
    logger: logger);

string response = await model.GenerateAsync("Explain RAG architecture");
```

---

### 3. Mistral AI 🌊
**Best for**: Cloud-based, cost-effective API access

**Characteristics**:
- ✅ **Fast inference** (cloud infrastructure)
- ✅ **Easy setup** (just API key)
- ✅ **Multiple models** (mistral-small, mistral-medium, mistral-large)
- ✅ **Competitive pricing** (~$0.001-0.01 per 1K tokens)
- ✅ **OpenAI-compatible** API
- ⚠️ **Requires internet** connection
- ⚠️ **Pay-per-use** (API costs)
- ⚠️ **Data sent to Mistral** servers

**Configuration**:
```json
{
  "OmniRAG": {
    "LanguageModel": {
      "Provider": "Mistral",
      "Mistral": {
        "ApiKey": "YOUR_MISTRAL_API_KEY_HERE",
        "ModelName": "mistral-small",
        "MaxTokens": 2048,
        "Temperature": 0.7
      }
    }
  }
}
```

**Setup Instructions**:
1. Create account at [mistral.ai](https://mistral.ai)
2. Generate API key from dashboard
3. Set `ApiKey` in configuration (or use environment variable)
4. Choose model: `mistral-small`, `mistral-medium`, or `mistral-large`
5. Run OmniRAG

**Code Example**:
```csharp
var logger = loggerFactory.CreateLogger<MistralLanguageModel>();
var model = new MistralLanguageModel(
    apiKey: "your-mistral-api-key",
    modelName: "mistral-small",
    maxTokens: 2048,
    temperature: 0.7f,
    logger: logger);

string response = await model.GenerateAsync("Summarize this document");
```

**Pricing** (as of Oct 2025):
- `mistral-small`: ~$0.001 per 1K tokens
- `mistral-medium`: ~$0.005 per 1K tokens
- `mistral-large`: ~$0.01 per 1K tokens

---

### 4. GPT (OpenAI) 🧠
**Best for**: Highest quality, state-of-the-art performance

**Characteristics**:
- ✅ **Best quality** (GPT-4 Turbo, GPT-4)
- ✅ **Fast inference** (cloud infrastructure)
- ✅ **Easy setup** (just API key)
- ✅ **Multiple models** (GPT-4, GPT-4 Turbo, GPT-3.5 Turbo)
- ✅ **Extensive documentation** and community support
- ⚠️ **Requires internet** connection
- ⚠️ **Higher cost** (~$0.01-0.03 per 1K tokens for GPT-4)
- ⚠️ **Data sent to OpenAI** servers

**Configuration**:
```json
{
  "OmniRAG": {
    "LanguageModel": {
      "Provider": "GPT",
      "GPT": {
        "ApiKey": "YOUR_OPENAI_API_KEY_HERE",
        "OrganizationId": "",
        "ModelName": "gpt-4-turbo",
        "MaxTokens": 2048,
        "Temperature": 0.7
      }
    }
  }
}
```

**Setup Instructions**:
1. Create account at [platform.openai.com](https://platform.openai.com)
2. Generate API key from dashboard
3. (Optional) Set Organization ID if using organization account
4. Set `ApiKey` in configuration (or use environment variable)
5. Choose model: `gpt-4-turbo`, `gpt-4`, or `gpt-3.5-turbo`
6. Run OmniRAG

**Code Example**:
```csharp
var logger = loggerFactory.CreateLogger<GptLanguageModel>();
var model = new GptLanguageModel(
    apiKey: "your-openai-api-key",
    modelName: "gpt-4-turbo",
    organizationId: null, // Optional
    maxTokens: 2048,
    temperature: 0.7f,
    logger: logger);

string response = await model.GenerateAsync("Analyze this technical document");
```

**Pricing** (as of Oct 2025):
- `gpt-3.5-turbo`: ~$0.001 per 1K tokens
- `gpt-4-turbo`: ~$0.01 per 1K tokens (input), ~$0.03 per 1K tokens (output)
- `gpt-4`: ~$0.03 per 1K tokens (input), ~$0.06 per 1K tokens (output)

---

## Using the Factory Pattern

### Automatic Model Selection (Recommended)
The factory automatically selects the model based on `appsettings.json`:

```csharp
using OmniRAG.Infrastructure.LanguageModels;

// In your startup/DI configuration
IConfiguration configuration = builder.Configuration;
ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddConsole());

ILanguageModel model = LanguageModelFactory.Create(configuration, loggerFactory);

// Use the model
string response = await model.GenerateAsync("What is machine learning?");
Console.WriteLine($"Using {model.ModelName}: {response}");
```

### Explicit Provider Selection
Override configuration at runtime:

```csharp
// Force GPT even if config says Phi4
ILanguageModel gptModel = LanguageModelFactory.Create("GPT", configuration, loggerFactory);

// Force Mistral for specific scenarios
ILanguageModel mistralModel = LanguageModelFactory.Create("Mistral", configuration, loggerFactory);
```

---

## Switching Between Models

### Change Configuration Only
Simply update `appsettings.json` and restart:

```json
{
  "OmniRAG": {
    "LanguageModel": {
      "Provider": "Mistral"  // Change from "Phi4" to "Mistral"
    }
  }
}
```

### Environment-Specific Models
Use different models per environment:

**appsettings.Development.json** (Local dev):
```json
{
  "OmniRAG": {
    "LanguageModel": {
      "Provider": "Phi4"  // Free local model for development
    }
  }
}
```

**appsettings.Production.json** (Production):
```json
{
  "OmniRAG": {
    "LanguageModel": {
      "Provider": "GPT"  // Best quality for production
    }
  }
}
```

### Dependency Injection Setup
Register in your DI container:

```csharp
// Program.cs or Startup.cs
services.AddSingleton<ILanguageModel>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    return LanguageModelFactory.Create(config, loggerFactory);
});
```

---

## Model Comparison Matrix

### Performance Characteristics

| Model | Latency | Quality | Context Window | Best Use Case |
|-------|---------|---------|----------------|---------------|
| **Phi-4** | 2-5s | Good | 4K tokens | Local, privacy-focused |
| **Llama-3-8B** | 2-5s | Very Good | 8K tokens | Local, high-quality |
| **Llama-3-70B** | 5-15s | Excellent | 8K tokens | Local, production-grade |
| **Mistral Small** | 0.5-1s | Very Good | 32K tokens | Cloud, cost-effective |
| **Mistral Large** | 0.5-1s | Excellent | 32K tokens | Cloud, high-quality |
| **GPT-3.5 Turbo** | 0.5-1s | Good | 16K tokens | Cloud, fast & cheap |
| **GPT-4 Turbo** | 1-3s | Excellent | 128K tokens | Cloud, best quality |

### Cost Comparison (1M tokens)

| Model | Input Cost | Output Cost | Total (Typical) |
|-------|-----------|-------------|-----------------|
| **Phi-4** | $0 | $0 | **$0** (Free) |
| **Llama** | $0 | $0 | **$0** (Free) |
| **Mistral Small** | ~$1 | ~$3 | **~$2** |
| **Mistral Medium** | ~$5 | ~$15 | **~$10** |
| **GPT-3.5 Turbo** | ~$1 | ~$2 | **~$1.50** |
| **GPT-4 Turbo** | ~$10 | ~$30 | **~$20** |

---

## Best Practices

### 1. Development vs. Production
```csharp
// Development: Use free local models
"Provider": "Phi4"  // No cost, good for testing

// Production: Use cloud models for reliability
"Provider": "GPT"   // Best quality, reliable uptime
```

### 2. Privacy-Sensitive Data
```csharp
// Always use local models for sensitive data
"Provider": "Phi4"  // or "Llama"
// Data never leaves your infrastructure
```

### 3. High-Volume Scenarios
```csharp
// Use cost-effective models
"Provider": "Mistral"  // ModelName: "mistral-small"
// Or local models to eliminate API costs entirely
```

### 4. Testing Strategy
```csharp
// Use local models for unit/integration tests
[Fact]
public async Task TestRagEngine()
{
    var config = CreateTestConfiguration("Phi4"); // Fast, no API calls
    var model = LanguageModelFactory.Create(config);
    // ... test logic
}
```

### 5. Fallback Strategy
```csharp
// Try primary model, fall back to secondary
ILanguageModel model;
try
{
    model = LanguageModelFactory.Create("GPT", config, loggerFactory);
}
catch
{
    // Fallback to local model if API unavailable
    model = LanguageModelFactory.Create("Phi4", config, loggerFactory);
}
```

---

## Resilience Patterns

All language models include production-grade resilience:

### Timeout Protection
```csharp
// Configured via appsettings.json
"Resilience": {
  "LanguageModel": {
    "TimeoutSeconds": 60  // Max wait time
  }
}
```

### Fallback on Failure
```csharp
// Automatic fallback to user-friendly message
"Resilience": {
  "LanguageModel": {
    "FallbackMessage": "[LLM unavailable] Please try again later."
  }
}
```

### Comprehensive Logging
All models log:
- Initialization status
- Generation parameters (tokens, temperature)
- Response times
- Errors and timeouts

---

## Troubleshooting

### Phi-4 Issues
```
❌ DirectoryNotFoundException: Phi-4 model directory not found
✅ Solution: Download model via AI Toolkit or set correct path
```

### Mistral API Issues
```
❌ InvalidOperationException: Mistral API key not specified
✅ Solution: Set ApiKey in appsettings.json or environment variable
```

### GPT API Issues
```
❌ 401 Unauthorized: Invalid API key
✅ Solution: Verify API key at platform.openai.com
```

### Llama ONNX Issues
```
❌ Model files not found
✅ Solution: Ensure ONNX model directory contains required files
```

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────┐
│           ILanguageModel Interface              │
│  (Strategy Pattern - All models interchangeable)│
└──────────────┬──────────────────────────────────┘
               │
       ┌───────┴────────┬──────────┬──────────┐
       │                │          │          │
┌──────▼─────┐  ┌──────▼─────┐  ┌─▼────┐  ┌──▼────┐
│Phi4Language│  │MistralLang │  │GPT   │  │Llama  │
│   Model    │  │uageModel   │  │Lang  │  │Language│
│            │  │            │  │Model │  │Model  │
│ (ONNX)     │  │ (API)      │  │(API) │  │(ONNX) │
└────────────┘  └────────────┘  └──────┘  └───────┘

         ┌──────────────────────────┐
         │ LanguageModelFactory     │
         │ (Creates models based on │
         │  appsettings.json)       │
         └──────────────────────────┘
```

---

## Summary

✅ **4 Language Models Implemented**: Phi-4, Llama, Mistral, GPT  
✅ **Strategy Pattern**: All implement `ILanguageModel`  
✅ **Factory Pattern**: Easy creation via `LanguageModelFactory`  
✅ **Configuration-Driven**: Switch models without code changes  
✅ **Resilience Built-in**: Timeout + Fallback for all models  
✅ **Production-Ready**: Comprehensive logging and error handling

Choose the model that best fits your needs:
- **Privacy/Cost**: Phi-4 or Llama (local, free)
- **Quality**: GPT-4 Turbo (best, expensive)
- **Balance**: Mistral (good quality, reasonable cost)

---

*Last Updated: October 3, 2025*  
*OmniRAG Version: 1.0*
