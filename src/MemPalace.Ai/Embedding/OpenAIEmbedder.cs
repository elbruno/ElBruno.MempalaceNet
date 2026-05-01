using MemPalace.Core.Backends;
using MemPalace.Core.Errors;
using OpenAI;
using OpenAI.Embeddings;

namespace MemPalace.Ai.Embedding;

/// <summary>
/// OpenAI embedder implementation with rate limiting and retry logic.
/// </summary>
public sealed class OpenAIEmbedder : ICustomEmbedder
{
    private readonly OpenAIClient _client;
    private readonly string _model;
    private readonly OpenAIOptions _options;
    private readonly SemaphoreSlim _rateLimiter;
    private int? _dimensions;

    public OpenAIEmbedder(OpenAIOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new ArgumentException(
                "OpenAI API key is required. Set OpenAIOptions.ApiKey or OPENAI_API_KEY environment variable.",
                nameof(options));
        }

        _client = new OpenAIClient(options.ApiKey);
        _model = options.Model ?? "text-embedding-3-small";
        _rateLimiter = new SemaphoreSlim(options.MaxRequestsPerMinute, options.MaxRequestsPerMinute);
    }

    public string ProviderName => "openai";
    public string ModelIdentity => $"openai:{_model}";
    
    public int Dimensions
    {
        get
        {
            if (!_dimensions.HasValue)
            {
                throw new InvalidOperationException(
                    "Dimensions not yet known. Call EmbedAsync at least once to infer dimensions.");
            }
            return _dimensions.Value;
        }
    }
    
    public IReadOnlyDictionary<string, object> Metadata => new Dictionary<string, object>
    {
        { "provider", ProviderName },
        { "model", _model },
        { "cost_per_1m_tokens", _options.CostPer1MTokens },
        { "max_rpm", _options.MaxRequestsPerMinute },
        { "max_retries", _options.MaxRetries }
    };

    public async ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default)
    {
        if (texts == null || texts.Count == 0)
        {
            return Array.Empty<ReadOnlyMemory<float>>();
        }

        // Rate limiting
        await _rateLimiter.WaitAsync(ct);
        
        try
        {
            // Retry logic with exponential backoff
            var embeddings = await RetryAsync(async () =>
            {
                try
                {
                    var response = await _client.GetEmbeddingClient(_model)
                        .GenerateEmbeddingsAsync(texts, cancellationToken: ct);
                    
                    var results = new List<ReadOnlyMemory<float>>(response.Value.Count);
                    foreach (var embedding in response.Value)
                    {
                        var vector = embedding.ToFloats().ToArray();
                        
                        // Infer dimensions from first embedding
                        if (!_dimensions.HasValue && vector.Length > 0)
                        {
                            _dimensions = vector.Length;
                        }
                        
                        results.Add(new ReadOnlyMemory<float>(vector));
                    }
                    
                    return (IReadOnlyList<ReadOnlyMemory<float>>)results;
                }
                catch (Exception ex) when (ex.Message.Contains("401") || ex.Message.Contains("Unauthorized"))
                {
                    throw new EmbedderError(
                        "Invalid OpenAI API key. Please verify your API key is correct and active. " +
                        "Set it via OpenAIOptions.ApiKey or OPENAI_API_KEY environment variable.",
                        ex);
                }
                catch (Exception ex) when (ex.Message.Contains("404") || ex.Message.Contains("not found"))
                {
                    throw new EmbedderError(
                        $"Model '{_model}' not found. Valid models: text-embedding-3-small, text-embedding-3-large, text-embedding-ada-002.",
                        ex);
                }
                catch (Exception ex)
                {
                    throw new EmbedderError(
                        $"OpenAI embedding failed: {ex.Message}",
                        ex);
                }
            }, ct);
            
            return embeddings;
        }
        finally
        {
            // Release rate limiter after delay
            _ = Task.Delay(TimeSpan.FromMinutes(1.0 / _options.MaxRequestsPerMinute), ct)
                .ContinueWith(_ => _rateLimiter.Release(), TaskScheduler.Default);
        }
    }

    private async Task<T> RetryAsync<T>(Func<Task<T>> operation, CancellationToken ct)
    {
        var attempt = 0;
        var delay = _options.InitialRetryDelay;
        
        while (true)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (
                attempt < _options.MaxRetries && 
                (ex.Message.Contains("429") || ex.Message.Contains("Rate limit")))
            {
                attempt++;
                await Task.Delay(delay, ct);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2); // Exponential backoff
            }
        }
    }
}

/// <summary>
/// Configuration options for OpenAI embedder.
/// </summary>
public sealed record OpenAIOptions
{
    /// <summary>
    /// OpenAI API key. Can also be set via OPENAI_API_KEY environment variable.
    /// </summary>
    public string? ApiKey { get; init; }
    
    /// <summary>
    /// Model name. Default: text-embedding-3-small.
    /// Valid options: text-embedding-3-small, text-embedding-3-large, text-embedding-ada-002.
    /// </summary>
    public string Model { get; init; } = "text-embedding-3-small";
    
    /// <summary>
    /// Maximum requests per minute (rate limiting). Default: 3500 (Tier 1).
    /// </summary>
    public int MaxRequestsPerMinute { get; init; } = 3500;
    
    /// <summary>
    /// Cost per 1 million tokens (for metadata). Default: $0.02 for text-embedding-3-small.
    /// </summary>
    public decimal CostPer1MTokens { get; init; } = 0.02m;
    
    /// <summary>
    /// Maximum number of retries for rate limit errors. Default: 3.
    /// </summary>
    public int MaxRetries { get; init; } = 3;
    
    /// <summary>
    /// Initial retry delay for exponential backoff. Default: 500ms.
    /// </summary>
    public TimeSpan InitialRetryDelay { get; init; } = TimeSpan.FromMilliseconds(500);
}
