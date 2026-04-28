# Embedder Pluggability Guide — MemPalace.NET

This guide shows you how to swap between embedders (local ONNX, OpenAI, Azure OpenAI, custom) and implement your own embedder for specialized use cases.

---

## Why Swap Embedders?

| Reason | Trade-off | Use Case |
|--------|-----------|----------|
| **Privacy** | Slower, limited to local models | Internal compliance, government, healthcare |
| **Cost** | Slightly slower inference per token | Free tier development, cost-sensitive deployments |
| **Scale** | Need horizontal embedder service | High-volume production with SLA requirements |
| **Quality** | Larger vectors = more memory/compute | Enterprise search requiring highest relevance |
| **Latency** | Local models are CPU-bound | Edge computing, sub-100ms response time SLAs |

---

## Available Embedders Comparison

| Embedder | Requires Setup | Runs Offline | Cost | Latency | Quality | Best For |
|----------|---|---|---|---|---|---|
| **Local (ONNX)** | ❌ No | ✅ Yes | $0 | 50-200ms | ⭐⭐⭐ | Development, privacy, learning |
| **OpenAI** | ✅ API key | ❌ No | $0.02-$0.13/1M tokens | 100-500ms | ⭐⭐⭐⭐ | Production, cloud-native apps |
| **Azure OpenAI** | ✅ Endpoint + Key | ❌ No | Usage-based | 100-500ms | ⭐⭐⭐⭐ | Enterprise, regulated compliance |
| **Custom** | ✅ Yes | 📋 Depends | Varies | Varies | Varies | Niche use cases, specialized models |

---

## Configuration Pattern

### 1. Local (Default) — ONNX-Based

**Zero setup**, runs entirely on your machine.

```csharp
using Microsoft.Extensions.DependencyInjection;
using MemPalace.Ai.Embedding;

var services = new ServiceCollection();

// Default configuration (no code needed)
services.AddMemPalaceAi();

// Or explicit
services.AddMemPalaceAi(options =>
{
    options.Type = EmbedderType.Local;
    options.Model = "sentence-transformers/all-MiniLM-L6-v2";  // Default
    options.MaxSequenceLength = 384;
});

var sp = services.BuildServiceProvider();
var embedder = sp.GetRequiredService<IEmbedder>();

// First use: downloads ~20-100 MB model (cached in ~/.cache/huggingface)
var embeddings = await embedder.EmbedAsync(new[] { "Hello world" });
```

**Available Models:**

```
sentence-transformers/all-MiniLM-L6-v2              (384 dims, fast, good balance)
sentence-transformers/all-mpnet-base-v2             (768 dims, slower, higher quality)
sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2  (384 dims, 50+ languages)
sentence-transformers/all-distilroberta-v1          (768 dims, multilingual)
```

**Pros:**
- ✅ No dependencies (single NuGet package)
- ✅ No API keys
- ✅ Privacy-first (data never leaves your machine)
- ✅ Free

**Cons:**
- ❌ Initial 1-2 minute model download
- ❌ CPU-bound (slower than cloud embedders)
- ❌ Limited to HuggingFace sentence-transformers

---

### 2. OpenAI

**Fastest option for production** with highest quality embeddings.

**Prerequisites:**
1. Create an [OpenAI account](https://platform.openai.com/signup)
2. Add billing and create an API key: https://platform.openai.com/api-keys
3. Set environment variable:

```bash
# macOS/Linux
export OPENAI_API_KEY="sk-..."

# Windows PowerShell
$env:OPENAI_API_KEY="sk-..."

# Windows Command Prompt
set OPENAI_API_KEY=sk-...
```

**Configuration:**

```csharp
using Microsoft.Extensions.DependencyInjection;
using MemPalace.Ai.Embedding;

var services = new ServiceCollection();

services.AddMemPalaceAi(options =>
{
    options.Type = EmbedderType.OpenAI;
    options.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    options.Model = "text-embedding-3-small";  // Default (recommended)
});

var sp = services.BuildServiceProvider();
var embedder = sp.GetRequiredService<IEmbedder>();

var embeddings = await embedder.EmbedAsync(new[] { "Hello world" });
```

**Supported Models:**

| Model | Dimensions | Cost (per 1M tokens) | Speed | Recommendation |
|-------|------------|----------------------|-------|---|
| `text-embedding-3-small` | 1536 | $0.02 | Fast | **Default** — cost-effective, high quality |
| `text-embedding-3-large` | 3072 | $0.13 | Medium | Premium — highest retrieval quality |
| `text-embedding-ada-002` | 1536 | $0.10 | Fast | Deprecated — avoid for new projects |

**Cost Example:**
- 1,000 memories × 200 tokens avg = 200K tokens
- `text-embedding-3-small`: 200K tokens ÷ 1M × $0.02 = **$0.004** (one-time)

**Pros:**
- ✅ Highest quality embeddings
- ✅ Fastest inference (~100ms)
- ✅ No local compute needed
- ✅ OpenAI actively maintains models

**Cons:**
- ❌ Requires API key (must pay)
- ❌ Data sent to OpenAI (privacy concern)
- ❌ Network dependency
- ❌ Rate limits (requests/minute)

---

### 3. Azure OpenAI

**Enterprise option** with compliance, SLAs, and data residency control.

**Prerequisites:**
1. Create an [Azure subscription](https://azure.microsoft.com)
2. Deploy Azure OpenAI resource and embedding model:
   - [Azure Portal](https://portal.azure.com) → Create resource → "Azure OpenAI"
   - Select region and deployment model (e.g., `text-embedding-ada-002`)
3. Copy endpoint and API key

**Configuration:**

```csharp
using Microsoft.Extensions.DependencyInjection;
using MemPalace.Ai.Embedding;

var services = new ServiceCollection();

services.AddMemPalaceAi(options =>
{
    options.Type = EmbedderType.AzureOpenAI;
    options.Endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
    options.ApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
    options.DeploymentName = "my-embedding-deployment";  // Your Azure deployment name
});

var sp = services.BuildServiceProvider();
var embedder = sp.GetRequiredService<IEmbedder>();

var embeddings = await embedder.EmbedAsync(new[] { "Hello world" });
```

**Environment Variables:**

```bash
# macOS/Linux
export AZURE_OPENAI_ENDPOINT="https://myresource.openai.azure.com"
export AZURE_OPENAI_API_KEY="abc123..."

# Windows PowerShell
$env:AZURE_OPENAI_ENDPOINT="https://myresource.openai.azure.com"
$env:AZURE_OPENAI_API_KEY="abc123..."
```

**Finding Your Deployment Name:**

1. [Azure Portal](https://portal.azure.com) → Your Azure OpenAI resource
2. Navigate to **Model deployments**
3. Copy the **Deployment name** column (e.g., "text-embedding-ada-002")

**Pros:**
- ✅ Enterprise SLA and support
- ✅ Data residency control (stays in your Azure region)
- ✅ Compliant for regulated industries (healthcare, finance)
- ✅ Integrated with Microsoft ecosystem

**Cons:**
- ❌ More complex setup
- ❌ Higher cost than OpenAI
- ❌ Requires Azure subscription
- ❌ Slower model updates

---

## Dependency Injection Setup — Code Examples

### Example 1: Detecting Environment at Runtime

```csharp
using Microsoft.Extensions.DependencyInjection;
using MemPalace.Ai.Embedding;

var services = new ServiceCollection();

// Detect which embedder to use based on environment
var embedderType = Environment.GetEnvironmentVariable("EMBEDDER_TYPE") ?? "Local";

switch (embedderType)
{
    case "OpenAI":
        services.AddMemPalaceAi(options =>
        {
            options.Type = EmbedderType.OpenAI;
            options.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            options.Model = "text-embedding-3-small";
        });
        break;
    
    case "AzureOpenAI":
        services.AddMemPalaceAi(options =>
        {
            options.Type = EmbedderType.AzureOpenAI;
            options.Endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            options.ApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
            options.DeploymentName = Environment.GetEnvironmentVariable("AZURE_DEPLOYMENT_NAME");
        });
        break;
    
    default:
        // Local (default)
        services.AddMemPalaceAi();
        break;
}

var sp = services.BuildServiceProvider();
```

### Example 2: Configuration File

```json
{
  "MemPalaceAi": {
    "EmbedderType": "OpenAI",
    "Model": "text-embedding-3-small",
    "ApiKey": "${OPENAI_API_KEY}"
  }
}
```

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MemPalace.Ai.Embedding;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();

// Bind configuration
services.AddMemPalaceAi(options =>
{
    options.Type = Enum.Parse<EmbedderType>(config["MemPalaceAi:EmbedderType"]);
    options.Model = config["MemPalaceAi:Model"];
    
    if (options.Type == EmbedderType.OpenAI)
        options.ApiKey = config["MemPalaceAi:ApiKey"];
});
```

### Example 3: Testing with Different Embedders

```csharp
using MemPalace.Ai.Embedding;
using MemPalace.Core.Backends;

public class MemorySearchTests
{
    // Local (unit tests)
    [Fact]
    public async Task Search_WithLocalEmbedder_ReturnsResults()
    {
        var services = new ServiceCollection();
        services.AddMemPalaceAi(opts => opts.Type = EmbedderType.Local);
        var sp = services.BuildServiceProvider();
        
        var embedder = sp.GetRequiredService<IEmbedder>();
        var embeddings = await embedder.EmbedAsync(new[] { "test" });
        
        Assert.NotEmpty(embeddings);
    }
    
    // OpenAI (integration tests, requires API key)
    [Fact(Skip = "Requires OPENAI_API_KEY")]
    public async Task Search_WithOpenAI_ReturnsPreciseResults()
    {
        var services = new ServiceCollection();
        services.AddMemPalaceAi(opts =>
        {
            opts.Type = EmbedderType.OpenAI;
            opts.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            opts.Model = "text-embedding-3-small";
        });
        var sp = services.BuildServiceProvider();
        
        var embedder = sp.GetRequiredService<IEmbedder>();
        var embeddings = await embedder.EmbedAsync(new[] { "test" });
        
        Assert.NotEmpty(embeddings);
    }
}
```

---

## Custom Embedder Implementation

Implement your own embedder for:
- **Specialized models** (domain-specific, proprietary)
- **Unsupported providers** (local vector database, niche APIs)
- **Hybrid approaches** (combine multiple embedders, custom logic)

### Interface Signature

```csharp
using MemPalace.Core.Backends;

public interface IEmbedder
{
    /// <summary>
    /// Unique identifier for the embedding model (e.g., "custom:my-model-v1").
    /// Must be stable across embedder instances.
    /// </summary>
    string ModelIdentity { get; }

    /// <summary>
    /// The dimensionality of embeddings produced by this model.
    /// Must match across all calls.
    /// </summary>
    int Dimensions { get; }

    /// <summary>
    /// Embeds a batch of texts into vectors.
    /// </summary>
    ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default);
}
```

### Minimal Custom Embedder

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
        var embeddings = new List<ReadOnlyMemory<float>>();

        foreach (var text in texts)
        {
            // Your embedding logic here
            var embedding = new float[Dimensions];
            
            // Example: Simple hash-based embedding (deterministic but not semantic)
            var hash = text.GetHashCode();
            var random = new Random(hash);
            for (int i = 0; i < Dimensions; i++)
                embedding[i] = (float)(random.NextDouble() * 2.0 - 1.0);
            
            // Normalize
            var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
            for (int i = 0; i < Dimensions; i++)
                embedding[i] /= (float)magnitude;

            embeddings.Add(embedding);
        }

        return embeddings;
    }
}

// Register in DI
var services = new ServiceCollection();
services.AddSingleton<IEmbedder>(new MyCustomEmbedder());
```

### Advanced: Wrap Microsoft.Extensions.AI Generator

If you have an existing `IEmbeddingGenerator<string, Embedding<float>>`:

```csharp
using MemPalace.Ai.Embedding;
using Microsoft.Extensions.AI;

public class MyAiGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    public EmbeddingGeneratorMetadata Metadata => new("my-generator");

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<Embedding<float>>();
        
        foreach (var text in values)
        {
            var vector = new float[512];
            // ... your logic ...
            results.Add(new Embedding<float>(vector));
        }

        return new GeneratedEmbeddings<Embedding<float>>(results);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public TService? GetService<TService>(object? key = null) where TService : class => null;
    public void Dispose() { }
}

// Wrap in MeaiEmbedder
var generator = new MyAiGenerator();
var embedder = new MeaiEmbedder(generator, "custom", "my-ai-model-v1");

var services = new ServiceCollection();
services.AddSingleton<IEmbedder>(embedder);
```

### Design Requirements

All custom embedders **must** satisfy:

1. **Stability**: `ModelIdentity` and `Dimensions` never change
2. **Consistency**: Same input → same output (modulo floating-point precision)
3. **Thread-safety**: Embedders are singletons; concurrent calls must be safe
4. **Determinism**: Avoid randomness in embeddings (affects reproducibility)

---

## Testing Your Embedder

### Unit Test Template

```csharp
using Xunit;
using MemPalace.Core.Backends;

public class MyEmbedderTests
{
    private readonly IEmbedder _embedder = new MyCustomEmbedder();

    [Fact]
    public void ModelIdentity_IsStable()
    {
        // ModelIdentity should be constant
        Assert.Equal("custom:my-embedder-v1", _embedder.ModelIdentity);
    }

    [Fact]
    public void Dimensions_IsConsistent()
    {
        // Dimensions should match expected
        Assert.Equal(512, _embedder.Dimensions);
    }

    [Fact]
    public async Task EmbedAsync_WithValidText_ReturnsCorrectDimensions()
    {
        var texts = new[] { "Hello world", "MemPalace.NET" };
        var embeddings = await _embedder.EmbedAsync(texts);

        Assert.Equal(2, embeddings.Count);
        foreach (var embedding in embeddings)
        {
            Assert.Equal(_embedder.Dimensions, embedding.Length);
        }
    }

    [Fact]
    public async Task EmbedAsync_WithSameInput_ReturnsSameEmbedding()
    {
        var text = "test";
        
        var emb1 = await _embedder.EmbedAsync(new[] { text });
        var emb2 = await _embedder.EmbedAsync(new[] { text });

        Assert.Equal(emb1[0].Span.ToArray(), emb2[0].Span.ToArray());
    }

    [Fact]
    public async Task EmbedAsync_WithEmptyList_ReturnsEmpty()
    {
        var embeddings = await _embedder.EmbedAsync(Array.Empty<string>());
        
        Assert.Empty(embeddings);
    }
}
```

### Integration Test: With Backend

```csharp
[Fact]
public async Task EndToEnd_StoreAndSearch_WithCustomEmbedder()
{
    var backend = new InMemoryBackend();
    var embedder = new MyCustomEmbedder();
    var palace = new PalaceRef("test-palace");

    // Create collection
    var collection = await backend.GetCollectionAsync(
        palace,
        "memories",
        create: true,
        embedder: embedder);

    // Store
    var record = new EmbeddedRecord(
        Id: "test-1",
        Document: "Hello world",
        Metadata: new Dictionary<string, object?>(),
        Embedding: (await embedder.EmbedAsync(new[] { "Hello world" }))[0]);

    await collection.UpsertAsync(new[] { record });

    // Search
    var queryEmbed = (await embedder.EmbedAsync(new[] { "Hello" }))[0];
    var results = await collection.QueryAsync(queryEmbed, topK: 1);

    Assert.Single(results.Ids);
    Assert.Equal("test-1", results.Ids[0]);
}
```

### Verification Checklist

- [ ] `ModelIdentity` returns consistent string
- [ ] `Dimensions` matches embedding vector length
- [ ] `EmbedAsync()` handles single and batch inputs
- [ ] Same input produces same output (determinism)
- [ ] Thread-safe for concurrent calls
- [ ] Proper exception handling for invalid inputs
- [ ] Unit tests pass
- [ ] Integration test with backend passes

---

## Troubleshooting

### Issue: "EmbedderIdentityMismatchException"

```
Collection was created with embedder "local:all-MiniLM-L6-v2" 
but you're trying to use "openai:text-embedding-3-small"
```

**Solution:** Embedders are locked at collection creation. To switch:
1. Create a new collection with the new embedder
2. Re-embed all documents
3. Delete the old collection

```csharp
// Old collection (don't use)
var oldCollection = await backend.GetCollectionAsync(palace, "old", create: false, embedder: localEmbedder);

// New collection with different embedder
var newCollection = await backend.GetCollectionAsync(palace, "new", create: true, embedder: openaiEmbedder);

// Copy data from old → new
var allRecords = await oldCollection.GetAsync(includeAll: true);
await newCollection.UpsertAsync(allRecords);

// Delete old
await backend.DeleteCollectionAsync(palace, "old");
```

### Issue: "Model Download Failed"

```
ElBruno.LocalEmbeddings: Failed to download model sentence-transformers/all-MiniLM-L6-v2
```

**Solution:**
1. Check internet connection
2. Verify model name is correct (see Available Models above)
3. Ensure disk space (typically 20-100 MB per model)
4. Manual cache: Download models from [HuggingFace Hub](https://huggingface.co/models)

```bash
# Force model download retry
rm -rf ~/.cache/huggingface/hub/models--sentence-transformers*
# Next call will re-download
```

### Issue: "Invalid API Key"

```
OpenAI: Unauthorized (401)
```

**Solution:**
1. Verify `OPENAI_API_KEY` environment variable is set
2. Test the key via OpenAI API:

```bash
curl https://api.openai.com/v1/models \
  -H "Authorization: Bearer sk-..."
```

### Issue: "Timeout / Network Error"

```
Request timeout after 30 seconds (OpenAI embedder)
```

**Solution:**
1. Check internet connectivity
2. Verify firewall/proxy not blocking OpenAI
3. Reduce batch size in code or increase timeout via options

---

## Next Steps

- **[Embedder Architecture](../embedder-architecture.md)** — Deep dive on design and extension
- **[Embedder Guide](../embedder-guide.md)** — Detailed provider reference
- **[AI Integration](../ai.md)** — Microsoft.Extensions.AI fundamentals
- **[Examples](../../examples/)** — Full working code samples

---

## Related Documentation

- [MemPalace Architecture](../architecture.md)
- [Backends Overview](../backends.md)
- [Search & Retrieval](../search.md)
