namespace MemPalace.Ai.Embedding;

/// <summary>
/// Configuration options for the embedder provider.
/// </summary>
public sealed record EmbedderOptions
{
    /// <summary>
    /// Embedding provider (Ollama, OpenAI, or AzureOpenAI).
    /// </summary>
    public string Provider { get; set; } = "Ollama";

    /// <summary>
    /// Model name (e.g., "nomic-embed-text" for Ollama).
    /// </summary>
    public string Model { get; set; } = "nomic-embed-text";

    /// <summary>
    /// API endpoint (defaults to local Ollama).
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:11434";

    /// <summary>
    /// API key for cloud providers (optional for Ollama).
    /// </summary>
    public string? ApiKey { get; set; }
}
