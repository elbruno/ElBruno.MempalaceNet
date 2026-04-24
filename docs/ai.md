# AI Integration — Microsoft.Extensions.AI

MemPalace.NET uses **Microsoft.Extensions.AI** (M.E.AI) as its abstraction layer for embedding models and LLM chat clients.

---

## Overview

The `MemPalace.Ai` library provides:
- **`MeaiEmbedder`** — Adapter wrapping M.E.AI's `IEmbeddingGenerator<string, Embedding<float>>` to implement MemPalace's `IEmbedder` interface (defined in `MemPalace.Core`).
- **Dependency injection support** — Easy registration via `AddMemPalaceAi(...)` extension method.
- **Provider abstraction** — Pluggable support for Local (default), Ollama, OpenAI, and Azure OpenAI.
- **Reranker skeleton** — LLM-based search result reranking (interface and stub implementation, full implementation in Phase 9).

---

## Default Provider: ElBruno.LocalEmbeddings (ONNX-based)

**As of the latest update**, MemPalace.NET uses **ElBruno.LocalEmbeddings** as the default embedding provider. This is an ONNX-based local embedder with zero external runtime dependencies and no API keys required.

**Why Local (ElBruno.LocalEmbeddings)?**  
- **Zero external dependencies**: No need to install Ollama or manage external services.
- **Privacy-first**: Embeddings are generated entirely on your machine with no data leaving your infrastructure.
- **No API keys or costs**: Completely free to use.
- **Fast startup**: ONNX model is downloaded and cached automatically on first use.
- **Microsoft.Extensions.AI compatible**: Implements `IEmbeddingGenerator<string, Embedding<float>>` so it plugs seamlessly into the M.E.AI ecosystem.

**Default model**: `sentence-transformers/all-MiniLM-L6-v2`  
- **Dimensions**: 384
- **Use case**: General-purpose sentence embeddings, good balance of quality and speed
- **Model cache**: Downloaded models are cached in your user directory (platform-dependent: `~/.cache/huggingface` on Linux/macOS, `%USERPROFILE%\.cache\huggingface` on Windows)

**Alternative providers** (Ollama, OpenAI, Azure OpenAI) are available as opt-in options — see below.

---

## Using the Embedder

### 1. Default (Local) — No Installation Required

```csharp
using Microsoft.Extensions.DependencyInjection;
using MemPalace.Ai.Embedding;

var services = new ServiceCollection();

// Use default (Local provider, all-MiniLM-L6-v2)
services.AddMemPalaceAi();

var sp = services.BuildServiceProvider();
var embedder = sp.GetRequiredService<IEmbedder>();
```

On first use, the ONNX model will be downloaded automatically and cached locally.

### 2. Embed Text

```csharp
var texts = new[] { "Hello world", "MemPalace.NET is great" };
var embeddings = await embedder.EmbedAsync(texts);

Console.WriteLine($"Model: {embedder.ModelIdentity}");  // "local:sentence-transformers/all-MiniLM-L6-v2"
Console.WriteLine($"Dimensions: {embedder.Dimensions}");  // 384
```

---

## Switching Embedding Models

### Use a Different Local Model

You can use any sentence-transformer model from HuggingFace:

```csharp
services.AddMemPalaceAi(options =>
{
    options.Provider = "Local";
    options.Model = "sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2";
    options.MaxSequenceLength = 128; // Optional: adjust tokenization length
});
```

**Popular alternatives:**
- `sentence-transformers/all-MiniLM-L6-v2` (384 dims, default, fast)
- `sentence-transformers/all-mpnet-base-v2` (768 dims, higher quality)
- `sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2` (384 dims, multilingual)

See the [ElBruno.LocalEmbeddings documentation](https://github.com/elbruno/elbruno.localembeddings) for the full list of supported models.

### Use Ollama (Opt-In)

If you prefer Ollama (requires separate Ollama installation):

```bash
# Install Ollama from https://ollama.ai/
ollama pull nomic-embed-text
```

```csharp
services.AddMemPalaceAi(options =>
{
    options.Provider = "Ollama";
    options.Model = "nomic-embed-text";
    options.Endpoint = "http://localhost:11434";
});
```

### OpenAI and Azure OpenAI (Phase 4+)

OpenAI and Azure OpenAI provider support is planned for Phase 4. Currently calling these will throw `NotImplementedException` with a message to use Local or Ollama. The DI registration API is ready:

```csharp
// OpenAI (not yet implemented)
services.AddMemPalaceAi(options =>
{
    options.Provider = "OpenAI";
    options.Model = "text-embedding-3-small";
    options.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
});

// Azure OpenAI (not yet implemented)
services.AddMemPalaceAi(options =>
{
    options.Provider = "AzureOpenAI";
    options.Model = "text-embedding-ada-002";
    options.Endpoint = "https://<your-resource>.openai.azure.com";
    options.ApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
});
```

---

## Embedder Identity

`MeaiEmbedder` exposes a `ModelIdentity` property combining the provider and model name:
- Local: `"local:sentence-transformers/all-MiniLM-L6-v2"` (or whatever model you configure)
- Ollama: `"ollama:nomic-embed-text"`
- Future: `"openai:text-embedding-3-small"`, `"azureopenai:text-embedding-ada-002"`

This identity is:
- Stored alongside vectors in the collection.
- Used by the backend to enforce consistency (throws `EmbedderIdentityMismatchException` if querying with a different embedder than the one used for indexing).

This matches Python MemPalace's `EmbedderIdentityMismatchError` behavior.

---

## Model Cache Location

When using the Local provider, ONNX models are cached in:
- **Windows**: `%USERPROFILE%\.cache\huggingface\hub`
- **Linux/macOS**: `~/.cache/huggingface/hub`

The first run downloads the model (typically 20-100 MB depending on the model). Subsequent runs load from cache instantly.

---

## Provider Comparison

| Provider | Default? | Requires Installation | API Key | Privacy | Notes |
|----------|----------|----------------------|---------|---------|-------|
| **Local** (ElBruno.LocalEmbeddings) | ✅ Yes | No | No | 100% local | ONNX-based, zero external dependencies, model auto-downloaded |
| **Ollama** | No | Yes (install Ollama) | No | 100% local | Requires Ollama service running |
| **OpenAI** | No | No | Yes | Cloud | Not yet implemented (Phase 4) |
| **Azure OpenAI** | No | No | Yes | Cloud | Not yet implemented (Phase 4) |

---

## Reranker (Skeleton, Phase 9 Full Implementation)

`MemPalace.Ai.Rerank` provides:
- **`IReranker`** interface: `RerankAsync(query, candidates) → IReadOnlyList<RankedHit>`.
- **`LlmReranker`** implementation taking an `IChatClient` (M.E.AI) — currently a pass-through stub; full LLM prompt implementation scheduled for Phase 9.

Usage (future):

```csharp
services.AddSingleton<IChatClient>(/* configure your chat model */);
services.AddSingleton<IReranker, LlmReranker>();

var reranker = sp.GetRequiredService<IReranker>();
var reranked = await reranker.RerankAsync(query, hits);
```

---

## Package Versions

Current packages (as of Local embedder integration):

| Package | Version | Notes |
|---------|---------|-------|
| `ElBruno.LocalEmbeddings` | 1.4.3 | Default local embedder (ONNX) |
| `Microsoft.Extensions.AI` | 9.5.0 | Stable |
| `Microsoft.Extensions.AI.Abstractions` | 10.4.1 | Stable (upgraded from 10.3.0 for ElBruno compatibility) |
| `Microsoft.Extensions.AI.Ollama` | 9.1.0-preview.1.25064.3 | Preview (stable Ollama provider not yet available) |
| `Microsoft.Extensions.AI.OpenAI` | 10.3.0 | Stable (integration deferred to Phase 4) |
| `Microsoft.Extensions.Options` | 10.0.5 | Stable (upgraded from 9.0.1 for ElBruno compatibility) |

---

## Testing

The test suite includes:
- **`MeaiEmbedderTests.cs`** — 11 tests using **NSubstitute** to mock `IEmbeddingGenerator` (all green, no live calls).
- **`LocalEmbedderRegistrationTests.cs`** — 7 tests verifying DI registration for Local provider (all green, no model downloads in tests).

**No live Ollama calls or model downloads** are made in tests (all mocked or DI-only tests).

---

## Next Steps

- **Phase 4**: Complete OpenAI/Azure OpenAI provider integration, end-to-end embedding → SQLite storage → query pipeline.
- **Phase 7**: MCP server integration for embedding APIs.
- **Phase 9**: Full LLM reranker with optimized prompts.
