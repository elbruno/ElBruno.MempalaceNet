# Embedder Architecture — MemPalace.NET

This document describes the design and extension points for MemPalace.NET's pluggable embedder system.

---

## Overview

MemPalace.NET uses a **three-layer architecture** for embedder pluggability:

```
┌─────────────────────────────────────────────────────────────┐
│ User Code (Consumers)                                       │
│ ─────────────────────────────────────────────────────────── │
│  IBackend.GetCollectionAsync(embedder: IEmbedder)          │
│  Palace.Create(config) → IEmbedder injected via DI          │
└─────────────────────────────────────────────────────────────┘
                          ▼
┌─────────────────────────────────────────────────────────────┐
│ MemPalace.Core (Abstractions)                               │
│ ─────────────────────────────────────────────────────────── │
│  IEmbedder interface                                         │
│    - string ModelIdentity                                    │
│    - int Dimensions                                          │
│    - ValueTask<ReadOnlyMemory<float>[]> EmbedAsync(...)     │
└─────────────────────────────────────────────────────────────┘
                          ▼
┌─────────────────────────────────────────────────────────────┐
│ MemPalace.Ai (M.E.AI Adapter Layer)                         │
│ ─────────────────────────────────────────────────────────── │
│  MeaiEmbedder (wraps IEmbeddingGenerator)                   │
│  AddMemPalaceAi(options) → registers IEmbedder              │
│  EmbedderOptions (Type enum, Model, ApiKey, etc.)           │
└─────────────────────────────────────────────────────────────┘
                          ▼
┌─────────────────────────────────────────────────────────────┐
│ Provider Implementations (M.E.AI-backed)                    │
│ ─────────────────────────────────────────────────────────── │
│  ✅ ElBruno.LocalEmbeddings → IEmbeddingGenerator           │
│  ✅ OpenAI (via M.E.AI.OpenAI) → IEmbeddingGenerator        │
│  ✅ Azure OpenAI (via M.E.AI.OpenAI) → IEmbeddingGenerator  │
│  ❌ Ollama (removed for v0.6 stable, preview-only)          │
└─────────────────────────────────────────────────────────────┘
```

---

## Core Interface: IEmbedder

The `IEmbedder` interface (in `MemPalace.Core.Backends`) is the primary abstraction:

```csharp
namespace MemPalace.Core.Backends;

public interface IEmbedder
{
    /// <summary>
    /// Unique identifier for the embedding model (e.g., "local:sentence-transformers/all-MiniLM-L6-v2").
    /// </summary>
    string ModelIdentity { get; }

    /// <summary>
    /// The dimensionality of the embeddings produced by this model.
    /// </summary>
    int Dimensions { get; }

    /// <summary>
    /// Embeds a list of texts into vectors.
    /// </summary>
    ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default);
}
```

### Design Rationale

- **ModelIdentity:** Used by backends to enforce embedder consistency across collection lifecycle
- **Dimensions:** Validated at collection creation; mismatch throws `DimensionMismatchException`
- **ReadOnlyMemory<float>:** Zero-copy vector representation (no unnecessary allocations)
- **Batch API:** All embedders accept multiple texts for efficiency (remote APIs amortize latency)

### Invariants

1. **Stability:** `ModelIdentity` and `Dimensions` must never change for a given embedder instance
2. **Consistency:** Two calls with the same input should produce identical embeddings (modulo numerical precision)
3. **Thread safety:** Embedders must be thread-safe (registered as singletons in DI)

---

## M.E.AI Integration Layer

MemPalace.NET builds on [Microsoft.Extensions.AI](https://learn.microsoft.com/en-us/dotnet/ai/) for embedder abstraction.

### MeaiEmbedder Adapter

```csharp
public sealed class MeaiEmbedder : IEmbedder
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;
    private readonly string _providerName;
    private readonly string _modelName;

    public MeaiEmbedder(
        IEmbeddingGenerator<string, Embedding<float>> generator,
        string providerName,
        string modelName)
    {
        _generator = generator;
        _providerName = providerName;
        _modelName = modelName;
    }

    public string ModelIdentity => $"{_providerName.ToLowerInvariant()}:{_modelName}";

    public int Dimensions { get; private set; } // Inferred from first embedding

    public async ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        var embeddings = await _generator.GenerateAsync(texts, cancellationToken: ct);
        // ... convert to ReadOnlyMemory<float>[] ...
    }
}
```

### Why M.E.AI?

- **Ecosystem compatibility:** Works with any `IEmbeddingGenerator` (OpenAI, Azure, custom)
- **Minimal dependencies:** Core interface lives in MemPalace.Core (no M.E.AI dependency)
- **Future-proof:** Microsoft.Extensions.AI is the standard .NET AI abstraction

---

## Provider Implementations

### Local Provider (ElBruno.LocalEmbeddings)

```csharp
services.AddMemPalaceAi(options =>
{
    options.Type = EmbedderType.Local;
    options.Model = "sentence-transformers/all-MiniLM-L6-v2";
});
```

**Implementation:**
- Uses [ElBruno.LocalEmbeddings](https://github.com/elbruno/LocalEmbeddings) NuGet package
- ONNX runtime for HuggingFace sentence-transformers models
- Auto-downloads models to `~/.cache/huggingface/hub`
- Implements `IEmbeddingGenerator<string, Embedding<float>>`

### OpenAI Provider

```csharp
services.AddMemPalaceAi(options =>
{
    options.Type = EmbedderType.OpenAI;
    options.ApiKey = "sk-...";
    options.Model = "text-embedding-3-small";
});
```

**Implementation:**
- Wraps `OpenAI.OpenAIClient` from OpenAI SDK
- Custom `OpenAIEmbeddingGenerator` implements `IEmbeddingGenerator`
- Calls `client.GetEmbeddingClient(model).GenerateEmbeddingsAsync(...)`

### Azure OpenAI Provider

```csharp
services.AddMemPalaceAi(options =>
{
    options.Type = EmbedderType.AzureOpenAI;
    options.Endpoint = "https://....openai.azure.com";
    options.ApiKey = "...";
    options.DeploymentName = "text-embedding-ada-002";
});
```

**Implementation:**
- Wraps `Azure.AI.OpenAI.AzureOpenAIClient`
- Custom `AzureOpenAIEmbeddingGenerator` implements `IEmbeddingGenerator`
- Calls `client.GetEmbeddingClient(deploymentName).GenerateEmbeddingsAsync(...)`

---

## Dependency Injection

### Registration Pattern

```csharp
public static IServiceCollection AddMemPalaceAi(
    this IServiceCollection services,
    Action<EmbedderOptions>? configure = null)
{
    // 1. Configure options
    services.Configure(configure ?? (_ => { }));

    // 2. Register IEmbeddingGenerator factory
    services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
    {
        var options = sp.GetRequiredService<IOptions<EmbedderOptions>>().Value;
        return options.Type switch
        {
            EmbedderType.Local => ...,      // ElBruno.LocalEmbeddings
            EmbedderType.OpenAI => ...,     // OpenAI wrapper
            EmbedderType.AzureOpenAI => ... // Azure wrapper
        };
    });

    // 3. Register IEmbedder adapter
    services.AddSingleton<IEmbedder>(sp =>
    {
        var generator = sp.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        var options = sp.GetRequiredService<IOptions<EmbedderOptions>>().Value;
        return new MeaiEmbedder(generator, options.Type.ToString(), options.Model);
    });

    return services;
}
```

### Lifetime: Singleton

All embedders are registered as **singletons** because:
- Model loading is expensive (Local provider caches ONNX models)
- Remote providers maintain HTTP client state
- Thread-safety is required by `IEmbedder` contract

---

## Extension Points

### 1. Custom IEmbedder Implementation

For embedders **not** compatible with M.E.AI:

```csharp
public class MyEmbedder : IEmbedder
{
    public string ModelIdentity => "custom:my-embedder-v1";
    public int Dimensions => 512;

    public async ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        // Your logic here
    }
}

// Register manually
services.AddSingleton<IEmbedder>(new MyEmbedder());
```

### 2. Custom IEmbeddingGenerator

For embedders compatible with M.E.AI but not built-in:

```csharp
public class MyGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    public EmbeddingGeneratorMetadata Metadata => new("custom");

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values, 
        EmbeddingGenerationOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        // Your logic here
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public TService? GetService<TService>(object? key = null) where TService : class => null;
    public void Dispose() { }
}

// Wrap in MeaiEmbedder
services.AddSingleton<IEmbedder>(
    new MeaiEmbedder(new MyGenerator(), "custom", "my-model-v1"));
```

### 3. Extend EmbedderOptions

For new providers, add enum value and factory logic:

```csharp
public enum EmbedderType
{
    Local,
    OpenAI,
    AzureOpenAI,
    MyNewProvider // Your addition
}

// In ServiceCollectionExtensions
return options.Type switch
{
    EmbedderType.Local => ...,
    EmbedderType.OpenAI => ...,
    EmbedderType.AzureOpenAI => ...,
    EmbedderType.MyNewProvider => CreateMyNewProviderGenerator(options),
    _ => throw new InvalidOperationException(...)
};
```

---

## Embedder Identity Enforcement

### Backend Contract

Backends (e.g., SqliteBackend) store `EmbedderIdentity` in collection metadata:

```sql
CREATE TABLE collections (
    name TEXT PRIMARY KEY,
    dimensions INTEGER NOT NULL,
    embedder_identity TEXT NOT NULL
);
```

When opening an existing collection:

```csharp
if (storedIdentity != embedder.ModelIdentity)
{
    throw new EmbedderIdentityMismatchException(
        $"Collection '{name}' was created with embedder '{storedIdentity}' " +
        $"but opened with '{embedder.ModelIdentity}'.");
}
```

### Why Enforce Identity?

- **Semantic consistency:** Embeddings from different models have incompatible geometry
- **Search correctness:** Cosine similarity only makes sense within same embedding space
- **Reproducibility:** Collection semantics depend on embedder choice

---

## Future Enhancements (v1.0+)

### 1. Dimension Adapters

Transform embeddings between different dimensions:

```csharp
public interface IEmbeddingAdapter
{
    ReadOnlyMemory<float> Adapt(ReadOnlyMemory<float> source, int targetDimensions);
}
```

Use cases:
- Migrate from 384-dim to 768-dim model
- Reduce dimensions for storage efficiency (PCA)

### 2. Embedding Cache

Cache embeddings to reduce API costs:

```csharp
public class CachedEmbedder : IEmbedder
{
    private readonly IEmbedder _inner;
    private readonly IDistributedCache _cache;

    public async ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(...)
    {
        // Check cache, call _inner if miss, store result
    }
}
```

### 3. Vector Store Backends

Store embeddings remotely (Qdrant, Pinecone, Milvus):

```csharp
public interface IVectorBackend : IBackend
{
    Task BulkUpsertAsync(IEnumerable<EmbeddedRecord> records);
    IAsyncEnumerable<QueryResult> StreamQueryAsync(ReadOnlyMemory<float> queryEmbedding, int limit);
}
```

### 4. Batch Optimization

Configurable batch sizes for API rate limits:

```csharp
public sealed record EmbedderOptions
{
    public int BatchSize { get; set; } = 100; // Split large batches
    public TimeSpan ThrottleDelay { get; set; } = TimeSpan.Zero; // Rate limit
}
```

---

## Testing Strategy

### Unit Tests

- **FakeEmbedder:** Deterministic hash-based embeddings for reproducible tests
- **Mock generators:** Test MeaiEmbedder without real embedders
- **DI validation:** Ensure correct registration for all provider types

### Integration Tests

- **Local provider:** Real ONNX model (model download in CI optional)
- **OpenAI/Azure:** Mock HTTP clients (no real API calls in CI)
- **End-to-end:** Store → embed → search round-trip with FakeEmbedder

---

## References

- **IEmbedder source:** `src/MemPalace.Core/Backends/IEmbedder.cs`
- **MeaiEmbedder source:** `src/MemPalace.Ai/Embedding/MeaiEmbedder.cs`
- **DI extensions:** `src/MemPalace.Ai/Embedding/ServiceCollectionExtensions.cs`
- **Microsoft.Extensions.AI:** https://learn.microsoft.com/en-us/dotnet/ai/
- **ElBruno.LocalEmbeddings:** https://github.com/elbruno/LocalEmbeddings

---

## License

MIT License — See [LICENSE](../LICENSE) for details.
