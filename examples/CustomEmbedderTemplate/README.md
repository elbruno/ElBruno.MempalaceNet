# Custom Embedder Template

This is a working template demonstrating how to implement a custom `IEmbedder` for MemPalace.NET.

## Overview

- **CustomEmbedder.cs** - Three example embedder implementations
- **Program.cs** - Example demonstrating embedder usage and validation

## Key Features

✓ Complete `IEmbedder` implementation  
✓ Deterministic embedding generation (for testing)  
✓ API-based embedder example (OpenAI, Cohere, etc.)  
✓ ONNX-based embedder example (local models)  
✓ Proper vector normalization for cosine similarity  
✓ Well-commented with adaptation guidance  

## How It Works

### CustomEmbedder
A deterministic embedder that produces the same embedding for the same input text. Useful for testing and validation.

```csharp
var embedder = new CustomEmbedder();
var embedding = await embedder.EmbedAsync(new[] { "hello" }, default);
// Same text always produces identical embeddings
```

### ApiBasedEmbedder
Template for integrating cloud-based embedders (OpenAI, Cohere, etc.).

```csharp
var embedder = new ApiBasedEmbedder(apiKey: "sk-...");
var embeddings = await embedder.EmbedAsync(new[] { "text1", "text2" }, default);
```

### OnnxEmbedder
Template for using local ONNX models without external dependencies.

```csharp
var embedder = new OnnxEmbedder("path/to/model.onnx");
var embeddings = await embedder.EmbedAsync(new[] { "text1", "text2" }, default);
```

## Running the Example

```bash
cd examples/CustomEmbedderTemplate
dotnet run
```

Expected output:
```
🏛️  MemPalace.NET - Custom Embedder Example

✓ Created collection with custom embedder: custom-embedder-template-v1
  Embedding dimensions: 128

📝 Adding records with custom embeddings...
✓ Added 3 records

🔍 Testing embedder consistency...
✓ Same text produces identical embeddings

🔍 Searching with custom embedder...
Results for 'machine learning algorithms':

  [1] (similarity: 0.876)
      Vector embeddings enable semantic similarity search

✅ Custom embedder example completed!
```

## Key Contract Requirements

Your embedder must satisfy these contracts:

### 1. Consistent ModelIdentity
```csharp
public string ModelIdentity => "my-embedder-v1";
// Must be the same for all records in a collection
```

### 2. Fixed Dimensions
```csharp
public int Dimensions => 384;  // e.g., 128, 384, 768, 1536
// All embeddings must have this exact length
```

### 3. Normalized Vectors
```csharp
// CRITICAL: Normalize embeddings to unit vectors for cosine similarity
var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
for (int i = 0; i < embedding.Length; i++)
{
    embedding[i] /= magnitude;
}
```

### 4. Deterministic Results
Same text should produce identical embeddings (important for testing):
```csharp
var emb1 = await embedder.EmbedAsync(new[] { "test" }, default);
var emb2 = await embedder.EmbedAsync(new[] { "test" }, default);
// emb1[0] must equal emb2[0]
```

## Adapting for Your Embedder

### Option 1: Using ONNX Models

```csharp
public class OnnxEmbedder : IEmbedder
{
    private readonly InferenceSession _session;
    private readonly Tokenizer _tokenizer;

    public async ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default)
    {
        var results = new List<ReadOnlyMemory<float>>();
        
        foreach (var text in texts)
        {
            // 1. Tokenize
            var tokens = _tokenizer.Encode(text);
            
            // 2. Run ONNX model
            var input = new Dictionary<string, OrtValue> 
            { 
                ["input_ids"] = OrtValue.CreateTensorValueFromMemory(tokens) 
            };
            var output = _session.Run(input);
            
            // 3. Extract and normalize
            var embedding = (float[])output[0].GetTensorData<float>();
            Normalize(embedding);
            
            results.Add(embedding);
        }
        
        return results;
    }
}
```

### Option 2: Using OpenAI API

```csharp
public class OpenAiEmbedder : IEmbedder
{
    private readonly HttpClient _httpClient;

    public async ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default)
    {
        var request = new { model = "text-embedding-3-small", input = texts };
        var response = await _httpClient.PostAsJsonAsync(
            "https://api.openai.com/v1/embeddings",
            request,
            cancellationToken: ct);
        
        var result = await response.Content.ReadAsAsync<OpenAiEmbeddingResponse>();
        
        return result.Data
            .OrderBy(d => d.Index)
            .Select(d => new ReadOnlyMemory<float>(d.Embedding.ToArray()))
            .ToList();
    }
}
```

### Option 3: Using Ollama (Local)

```csharp
public class OllamaEmbedder : IEmbedder
{
    private readonly HttpClient _httpClient;

    public async ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default)
    {
        var results = new List<ReadOnlyMemory<float>>();
        
        foreach (var text in texts)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "http://localhost:11434/api/embed",
                new { model = "nomic-embed-text", prompt = text },
                cancellationToken: ct);
            
            var result = await response.Content.ReadAsAsync<OllamaEmbedResponse>();
            results.Add(new ReadOnlyMemory<float>(result.Embedding.ToArray()));
        }
        
        return results;
    }
}
```

## Validation Checklist

- [ ] `ModelIdentity` is set correctly
- [ ] `Dimensions` matches your embedding model's output size
- [ ] All embeddings are normalized to unit vectors
- [ ] Same text produces identical embeddings (deterministic)
- [ ] Batch embedding works correctly
- [ ] Error handling for API failures (if applicable)
- [ ] Cancellation tokens are respected
- [ ] Can be registered in DI container

## Common Pitfalls

❌ **Not normalizing vectors:**
```csharp
// WRONG: Will give inconsistent cosine distances
return embedding;

// RIGHT: Normalize first
var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
for (int i = 0; i < embedding.Length; i++)
    embedding[i] /= magnitude;
return embedding;
```

❌ **Changing dimensions per-call:**
```csharp
// WRONG: Breaks collection consistency
return random_dimension_count_embedding;

// RIGHT: Always return exact Dimensions
return new float[Dimensions];
```

❌ **Non-deterministic results:**
```csharp
// WRONG: Each call produces different output for same input
using var random = new Random();
for (int i = 0; i < Dimensions; i++)
    embedding[i] = (float)random.NextDouble();

// RIGHT: Use input text to seed randomness
var random = new Random(text.GetHashCode());
```

## Links

- **Library Guide:** [../../../docs/guides/csharp-library-developers.md](../../../docs/guides/csharp-library-developers.md)
- **IEmbedder Interface:** [../../../src/MemPalace.Core/Backends/IEmbedder.cs](../../../src/MemPalace.Core/Backends/IEmbedder.cs)
- **SimpleMemoryAgent Example:** [../SimpleMemoryAgent/Program.cs](../SimpleMemoryAgent/Program.cs)
- **ONNX Runtime:** https://github.com/microsoft/onnxruntime
- **OpenAI .NET:** https://github.com/openai/openai-dotnet
- **Ollama:** https://ollama.ai
