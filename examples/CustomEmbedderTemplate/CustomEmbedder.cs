using MemPalace.Core.Backends;

namespace CustomEmbedderTemplate;

/// <summary>
/// Example custom embedder implementation.
/// 
/// This demonstrates the minimal contract for IEmbedder.
/// It uses a deterministic hash-based approach for testing (not suitable for production).
/// 
/// To adapt this for a real embedder:
/// 1. Replace the embedding logic with calls to your model (ONNX, API, etc.)
/// 2. Ensure embeddings are normalized for consistent cosine similarity
/// 3. Use ValueTask for async I/O (if calling external services)
/// 4. Set proper ModelIdentity to match your model name/version
/// 
/// Common embedder sources:
/// - ONNX models: Use Microsoft.ML.OnnxRuntime
/// - OpenAI: Use OpenAI .NET client
/// - Ollama: Use HTTP client with ollama API
/// - Local models: Use Hugging Face transformers
/// </summary>
public sealed class CustomEmbedder : IEmbedder
{
    /// <summary>
    /// Unique model identifier. Used to validate collection consistency.
    /// Must be the same for all records in a collection.
    /// Example: "nomic-embed-text-v1.5", "text-embedding-3-small", "all-MiniLM-L6-v2"
    /// </summary>
    public string ModelIdentity => "custom-embedder-template-v1";

    /// <summary>
    /// Output embedding dimensionality. Must be consistent for all texts.
    /// Examples: 384 (small), 768 (medium), 1536 (large like OpenAI)
    /// </summary>
    public int Dimensions => 128;

    /// <summary>
    /// Embeds multiple texts into vectors in a single call.
    /// This is the core contract: accepts list of strings, returns list of float arrays.
    /// 
    /// Example with ONNX:
    /// 1. Tokenize input texts
    /// 2. Run ONNX session with tokens
    /// 3. Extract embedding output
    /// 4. Normalize to unit vectors
    /// 5. Return as ReadOnlyMemory<float>
    /// </summary>
    public async ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default)
    {
        var results = new List<ReadOnlyMemory<float>>();

        foreach (var text in texts)
        {
            // IMPORTANT: Your embedding logic goes here.
            // For this template, we use a simple deterministic hash-based approach.
            
            var embedding = ComputeEmbedding(text);
            results.Add(embedding);
        }

        return await ValueTask.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(results);
    }

    /// <summary>
    /// Computes a deterministic embedding for a single text.
    /// In production, replace this with your actual model.
    /// </summary>
    private ReadOnlyMemory<float> ComputeEmbedding(string text)
    {
        var embedding = new float[Dimensions];

        // Deterministic: same text always produces same embedding
        var hash = text.GetHashCode();
        var random = new Random(hash);

        // Generate random values in [-1, 1]
        for (int i = 0; i < Dimensions; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2.0 - 1.0);
        }

        // CRITICAL: Normalize to unit vector for cosine similarity
        // This ensures all embeddings have magnitude 1, making cosine distance valid
        var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < Dimensions; i++)
            {
                embedding[i] /= magnitude;
            }
        }

        return embedding;
    }
}

/// <summary>
/// Alternative example: wrapping an external API (e.g., OpenAI).
/// Demonstrates async/await with external service calls.
/// </summary>
public sealed class ApiBasedEmbedder : IEmbedder
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelName;

    public string ModelIdentity => $"openai-{_modelName}";
    public int Dimensions => 1536;  // text-embedding-3-large

    public ApiBasedEmbedder(string apiKey, string modelName = "text-embedding-3-small")
    {
        _apiKey = apiKey;
        _modelName = modelName;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    public async ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default)
    {
        // Example API call structure (pseudo-code):
        // 
        // var requestBody = new { model = _modelName, input = texts };
        // var response = await _httpClient.PostAsJsonAsync(
        //     "https://api.openai.com/v1/embeddings",
        //     requestBody,
        //     ct);
        //
        // var result = await response.Content.ReadAsAsync<OpenAiEmbeddingResponse>(cancellationToken: ct);
        // return result.Data.Select(d => new ReadOnlyMemory<float>(d.Embedding.ToArray())).ToList();

        // For this template, we'll just throw NotImplemented
        throw new NotImplementedException(
            "Replace this with your actual API call. See comments above for example structure.");
    }
}

/// <summary>
/// Example: ONNX-based embedder (local, no API key needed).
/// Demonstrates working with pre-downloaded models.
/// </summary>
public sealed class OnnxEmbedder : IEmbedder
{
    private readonly string _modelPath;

    public string ModelIdentity => "onnx-nomic-embed-text-v1.5";
    public int Dimensions => 768;

    public OnnxEmbedder(string modelPath)
    {
        _modelPath = modelPath;
        // Example: Load ONNX session from _modelPath
        // var session = new InferenceSession(_modelPath);
    }

    public async ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default)
    {
        // Example structure:
        //
        // var results = new List<ReadOnlyMemory<float>>();
        // foreach (var text in texts)
        // {
        //     // 1. Tokenize
        //     var tokens = _tokenizer.Encode(text);
        //
        //     // 2. Run ONNX session
        //     var inputs = new List<NamedOnnxValue>
        //     {
        //         NamedOnnxValue.CreateFromTensor("input_ids", tensor)
        //     };
        //     var outputs = _session.Run(inputs);
        //
        //     // 3. Extract embedding
        //     var embedding = (float[])outputs.First().Value;
        //
        //     // 4. Normalize
        //     Normalize(embedding);
        //
        //     results.Add(embedding);
        // }
        // return results;

        throw new NotImplementedException(
            "Replace this with actual ONNX inference. See comments above for example structure.");
    }
}
