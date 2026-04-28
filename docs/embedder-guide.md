# Embedder Guide — MemPalace.NET

This guide shows you how to use MemPalace.NET's pluggable embedder system to switch between local ONNX embeddings, OpenAI, Azure OpenAI, or your own custom embedders.

---

## Quick Start

MemPalace.NET defaults to **local-first** ONNX embeddings via [ElBruno.LocalEmbeddings](https://github.com/elbruno/LocalEmbeddings). No API keys required, everything runs on your machine.

```csharp
using MemPalace.Ai.Embedding;
using MemPalace.Core.Backends;
using Microsoft.Extensions.DependencyInjection;

// Default: Local ONNX embeddings
services.AddMemPalaceAi();

// Get embedder
var embedder = provider.GetRequiredService<IEmbedder>();
```

---

## Provider Comparison

| Provider | Pros | Cons | Use Case |
|----------|------|------|----------|
| **Local (ONNX)** | ✅ No API keys<br>✅ Runs offline<br>✅ Privacy-first<br>✅ No costs | ❌ Initial model download (~20-100 MB)<br>❌ Slower than remote APIs<br>❌ Limited to HuggingFace models | Development, privacy-sensitive apps, offline scenarios |
| **OpenAI** | ✅ Fast<br>✅ High-quality embeddings<br>✅ No local compute | ❌ Requires API key<br>❌ Costs money<br>❌ Data leaves your machine | Production apps, cloud deployments |
| **Azure OpenAI** | ✅ Enterprise SLA<br>✅ Data residency control<br>✅ High-quality | ❌ Requires Azure subscription<br>❌ More complex setup | Enterprise production apps |

---

## Local Provider (ElBruno.LocalEmbeddings)

### Default Configuration

```csharp
services.AddMemPalaceAi();
// Uses: sentence-transformers/all-MiniLM-L6-v2 (384 dimensions)
```

### Custom Model

```csharp
services.AddMemPalaceAi(options =>
{
    options.Type = EmbedderType.Local;
    options.Model = "sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2";
    options.MaxSequenceLength = 512;
});
```

### Available Models

MemPalace.NET supports any HuggingFace sentence-transformers model. Popular choices:

| Model | Dimensions | Language | Speed | Quality |
|-------|------------|----------|-------|---------|
| `all-MiniLM-L6-v2` | 384 | English | Fast | Good |
| `all-mpnet-base-v2` | 768 | English | Medium | Better |
| `paraphrase-multilingual-MiniLM-L12-v2` | 384 | 50+ | Medium | Good |

**First-time use:** Model downloads automatically to `~/.cache/huggingface/hub` (Windows: `%USERPROFILE%\.cache\huggingface\hub`).

---

## OpenAI Provider

### API Key Configuration

Set via environment variable or code:

```bash
# Environment variable (recommended)
export OPENAI_API_KEY="sk-..."
```

```csharp
// Code configuration
services.AddMemPalaceAi(options =>
{
    options.Type = EmbedderType.OpenAI;
    options.Model = "text-embedding-3-small"; // Default
    options.ApiKey = "sk-..."; // Or use environment variable
});
```

### Supported Models

| Model | Dimensions | Cost (per 1M tokens) | Use Case |
|-------|------------|----------------------|----------|
| `text-embedding-3-small` | 1536 | $0.02 | General-purpose, cost-effective |
| `text-embedding-3-large` | 3072 | $0.13 | High-quality, better retrieval |
| `text-embedding-ada-002` | 1536 | $0.10 | Legacy (still supported) |

### Example

```csharp
using MemPalace.Ai.Embedding;

services.AddMemPalaceAi(options =>
{
    options.Type = EmbedderType.OpenAI;
    options.Model = "text-embedding-3-small";
});

var embedder = provider.GetRequiredService<IEmbedder>();
var embeddings = await embedder.EmbedAsync(new[] { "Hello world" });
```

---

## Azure OpenAI Provider

### Configuration

Requires:
- **Endpoint**: Your Azure OpenAI resource URL
- **API Key**: Found in Azure Portal → Keys and Endpoint
- **Deployment Name**: Your embedding model deployment name

```bash
# Environment variables (recommended)
export AZURE_OPENAI_API_KEY="..."
export AZURE_OPENAI_ENDPOINT="https://YOUR_RESOURCE.openai.azure.com"
```

```csharp
services.AddMemPalaceAi(options =>
{
    options.Type = EmbedderType.AzureOpenAI;
    options.Endpoint = "https://YOUR_RESOURCE.openai.azure.com";
    options.ApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
    options.DeploymentName = "text-embedding-ada-002"; // Your deployment name
});
```

### Finding Your Deployment Name

1. Open [Azure Portal](https://portal.azure.com)
2. Navigate to your Azure OpenAI resource
3. Go to **Model deployments**
4. Copy the **Deployment name** (not the model name)

---

## Custom Embedders

You can plug in your own embedder by implementing `IEmbedder`:

### Option 1: Direct Implementation

```csharp
using MemPalace.Core.Backends;

public class MyCustomEmbedder : IEmbedder
{
    public string ModelIdentity => "custom:my-embedder-v1";
    public int Dimensions => 512;
    
    public async ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> texts, 
        CancellationToken ct = default)
    {
        // Your embedding logic here
        var embeddings = new List<ReadOnlyMemory<float>>();
        foreach (var text in texts)
        {
            // Generate embedding (example: random)
            var embedding = new float[Dimensions];
            // ... your logic ...
            embeddings.Add(embedding);
        }
        return embeddings;
    }
}

// Register
services.AddSingleton<IEmbedder>(new MyCustomEmbedder());
```

### Option 2: Wrap M.E.AI Generator

If you have an `IEmbeddingGenerator<string, Embedding<float>>`:

```csharp
using MemPalace.Ai.Embedding;

var myGenerator = ...; // Your IEmbeddingGenerator
services.AddSingleton<IEmbedder>(
    new MeaiEmbedder(myGenerator, "custom", "my-model-v1"));
```

---

## Switching Embedders

**Important:** Once a collection is created with an embedder, you cannot switch embedders. The embedder identity is stored in the collection metadata.

```csharp
var collection = await backend.GetCollectionAsync(
    palaceRef, 
    collectionName, 
    create: true, 
    embedder: embedder);
```

If you try to open an existing collection with a different embedder, you'll get an `EmbedderIdentityMismatchException`.

**Migration strategy:**
1. Create a new collection with the new embedder
2. Re-embed all documents
3. Delete the old collection

---

## Error Handling

### Missing API Key

```csharp
// Throws: InvalidOperationException
services.AddMemPalaceAi(options =>
{
    options.Type = EmbedderType.OpenAI;
    // No API key set
});
```

**Fix:** Set `OPENAI_API_KEY` environment variable or `options.ApiKey`.

### Dimension Mismatch

```csharp
// Collection created with 384-dim embeddings
// Trying to use 1536-dim embeddings
// Throws: EmbedderIdentityMismatchException
```

**Fix:** Use the same embedder or create a new collection.

### Model Download Failure

```
ElBruno.LocalEmbeddings: Failed to download model
```

**Fix:** Check internet connection, verify model name, ensure disk space.

---

## Performance Tips

### Local Provider

- **First run is slow:** Model downloads (~20-100 MB). Subsequent runs load from cache instantly.
- **Batch embeddings:** Pass multiple texts at once for better throughput.
- **GPU acceleration:** Not yet supported (CPU-only ONNX runtime).

### Remote Providers (OpenAI, Azure)

- **Rate limits:** OpenAI has tier-based rate limits (check your account tier).
- **Batch size:** OpenAI supports up to 2048 inputs per request.
- **Retries:** Implement exponential backoff for transient failures.

---

## Next Steps

- **Architecture Details:** See [embedder-architecture.md](./embedder-architecture.md)
- **CLI Configuration:** See [cli-embedder-config.md](./cli-embedder-config.md)
- **AI Integration:** See [ai.md](./ai.md)

---

## FAQ

### Q: Can I use OpenAI embeddings offline?

**A:** No. OpenAI requires an internet connection to their API. Use the Local provider for offline scenarios.

### Q: Are embeddings cached?

**A:** Not yet. Every `EmbedAsync` call generates fresh embeddings. Caching is planned for v1.0.

### Q: Can I switch embedders after creating a collection?

**A:** No. Collections store the embedder identity and enforce consistency. You must create a new collection with the new embedder.

### Q: Which provider is cheapest?

**A:** Local provider is free (no API costs). OpenAI `text-embedding-3-small` is cheapest remote option ($0.02 per 1M tokens).

### Q: How do I know which embedder a collection uses?

**A:** Check `collection.EmbedderIdentity` property:

```csharp
var identity = collection.EmbedderIdentity;
// Example: "local:sentence-transformers/all-MiniLM-L6-v2"
```

---

## License

MIT License — See [LICENSE](../LICENSE) for details.
