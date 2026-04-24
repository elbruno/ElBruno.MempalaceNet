# AI Integration — Microsoft.Extensions.AI

MemPalace.NET uses **Microsoft.Extensions.AI** (M.E.AI) as its abstraction layer for embedding models and LLM chat clients.

---

## Overview

The `MemPalace.Ai` library provides:
- **`MeaiEmbedder`** — Adapter wrapping M.E.AI's `IEmbeddingGenerator<string, Embedding<float>>` to implement MemPalace's `IEmbedder` interface (defined in `MemPalace.Core`).
- **Dependency injection support** — Easy registration via `AddMemPalaceAi(...)` extension method.
- **Provider abstraction** — Pluggable support for Ollama (default), OpenAI, and Azure OpenAI.
- **Reranker skeleton** — LLM-based search result reranking (interface and stub implementation, full implementation in Phase 9).

---

## Default Provider: Ollama

Phase 3 ships with **Ollama** as the default embedding provider, using the `nomic-embed-text` model running locally at `http://localhost:11434`.

**Why Ollama?**  
- **Local-first**: no API keys, no usage costs, no data leaving your machine.
- **Privacy**: embeddings stay on your infrastructure.
- **Fast**: `nomic-embed-text` is lightweight and fast for local use.

---

## Using the Embedder

### 1. Install Ollama and Pull the Model

```bash
# Install Ollama from https://ollama.ai/
ollama pull nomic-embed-text
```

### 2. Register in DI Container

```csharp
using Microsoft.Extensions.DependencyInjection;
using MemPalace.Ai.Embedding;

var services = new ServiceCollection();

// Use default (Ollama, nomic-embed-text, http://localhost:11434)
services.AddMemPalaceAi();

// OR configure explicitly
services.AddMemPalaceAi(options =>
{
    options.Provider = "Ollama";
    options.Model = "nomic-embed-text";
    options.Endpoint = "http://localhost:11434";
});

var sp = services.BuildServiceProvider();
var embedder = sp.GetRequiredService<IEmbedder>();
```

### 3. Embed Text

```csharp
var texts = new[] { "Hello world", "MemPalace.NET is great" };
var embeddings = await embedder.EmbedAsync(texts);

Console.WriteLine($"Model: {embedder.ModelIdentity}");  // "ollama:nomic-embed-text"
Console.WriteLine($"Dimensions: {embedder.Dimensions}");  // 768 (for nomic-embed-text)
```

---

## Switching Embedding Models

### Use a Different Ollama Model

```csharp
services.AddMemPalaceAi(options =>
{
    options.Provider = "Ollama";
    options.Model = "mxbai-embed-large";  // or any model you've pulled
    options.Endpoint = "http://localhost:11434";
});
```

### OpenAI and Azure OpenAI (Phase 4+)

OpenAI and Azure OpenAI provider support is planned for Phase 4. Currently calling these will throw `NotImplementedException` with a message to use Ollama. The DI registration API is ready:

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

`MeaiEmbedder` exposes a `ModelIdentity` property combining the provider and model name (e.g., `"ollama:nomic-embed-text"`). This identity is:
- Stored alongside vectors in the collection.
- Used by the backend to enforce consistency (throws `EmbedderIdentityMismatchException` if querying with a different embedder than the one used for indexing).

This matches Python MemPalace's `EmbedderIdentityMismatchError` behavior.

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

## Package Versions (Phase 3)

Current Microsoft.Extensions.AI packages (as of Phase 3 delivery):

| Package | Version | Notes |
|---------|---------|-------|
| `Microsoft.Extensions.AI` | 9.5.0 | Stable |
| `Microsoft.Extensions.AI.Abstractions` | 10.3.0 | Stable |
| `Microsoft.Extensions.AI.Ollama` | 9.1.0-preview.1.25064.3 | Preview (stable Ollama provider not yet available) |
| `Microsoft.Extensions.AI.OpenAI` | 10.3.0 | Stable (integration deferred to Phase 4) |

The Ollama package version requires preview because stable 1.x packages don't yet exist. OpenAI/Azure support is stubbed but not yet functional (requires compatible M.E.AI.OpenAI APIs).

---

## Testing

Phase 3 includes a full test suite (`MemPalace.Tests/Ai/MeaiEmbedderTests.cs`) using **NSubstitute** to mock `IEmbeddingGenerator`. Tests verify:
- ModelIdentity construction
- Dimension inference from first embedding
- Batching
- ReadOnlyMemory<float> round-tripping
- CancellationToken propagation

**No live Ollama calls** are made in tests (all mocked).

---

## Next Steps

- **Phase 4**: Complete OpenAI/Azure OpenAI provider integration, end-to-end embedding → SQLite storage → query pipeline.
- **Phase 7**: MCP server integration for embedding APIs.
- **Phase 9**: Full LLM reranker with optimized prompts.
