namespace MemPalace.Ai.Embedding;

/// <summary>
/// Configuration options for the embedder provider.
/// </summary>
public sealed record EmbedderOptions
{
    /// <summary>
    /// Embedding provider (Local, Ollama, OpenAI, or AzureOpenAI).
    /// </summary>
    public string Provider { get; set; } = "Local";

    /// <summary>
    /// Model name.
    /// - Local: HuggingFace model ID (default: "sentence-transformers/all-MiniLM-L6-v2")
    /// - Ollama: model name (e.g., "nomic-embed-text")
    /// - OpenAI/AzureOpenAI: deployment/model name
    /// </summary>
    public string Model { get; set; } = "sentence-transformers/all-MiniLM-L6-v2";

    /// <summary>
    /// API endpoint (for Ollama/OpenAI/AzureOpenAI, ignored for Local).
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:11434";

    /// <summary>
    /// API key for cloud providers (not used for Local or Ollama).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Maximum sequence length for tokenization (Local embedder only).
    /// Default is 256 for all-MiniLM-L6-v2.
    /// </summary>
    public int MaxSequenceLength { get; set; } = 256;
}
