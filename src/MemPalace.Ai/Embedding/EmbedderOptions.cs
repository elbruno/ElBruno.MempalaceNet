namespace MemPalace.Ai.Embedding;

/// <summary>
/// Embedding provider type.
/// </summary>
public enum EmbedderType
{
    /// <summary>
    /// Local ONNX embeddings via ElBruno.LocalEmbeddings (default, no API keys required).
    /// </summary>
    Local,
    
    /// <summary>
    /// OpenAI remote embeddings API (requires API key).
    /// </summary>
    OpenAI,
    
    /// <summary>
    /// Azure OpenAI remote embeddings API (requires API key and deployment name).
    /// </summary>
    AzureOpenAI
}

/// <summary>
/// Configuration options for the embedder provider.
/// </summary>
public sealed record EmbedderOptions
{
    /// <summary>
    /// Embedding provider type (Local, OpenAI, or AzureOpenAI).
    /// Default: Local (ElBruno.LocalEmbeddings).
    /// </summary>
    public EmbedderType Type { get; set; } = EmbedderType.Local;

    /// <summary>
    /// Embedding provider (for backward compatibility, prefer Type property).
    /// Supported values: "Local", "OpenAI", "AzureOpenAI".
    /// </summary>
    [Obsolete("Use Type property instead. This will be removed in v1.0.")]
    public string Provider { get; set; } = "Local";

    /// <summary>
    /// Model name.
    /// - Local: HuggingFace model ID (default: "sentence-transformers/all-MiniLM-L6-v2")
    /// - OpenAI: model name (e.g., "text-embedding-3-small", "text-embedding-3-large")
    /// - AzureOpenAI: model name (e.g., "text-embedding-ada-002")
    /// </summary>
    public string Model { get; set; } = "sentence-transformers/all-MiniLM-L6-v2";

    /// <summary>
    /// API endpoint (for OpenAI/AzureOpenAI, ignored for Local).
    /// OpenAI default: https://api.openai.com/v1
    /// Azure: your Azure OpenAI endpoint URL
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// API key for cloud providers (OpenAI, AzureOpenAI).
    /// Not used for Local provider.
    /// Can also be set via environment variable: OPENAI_API_KEY or AZURE_OPENAI_API_KEY
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Deployment name (Azure OpenAI only).
    /// Required when Type is AzureOpenAI.
    /// </summary>
    public string? DeploymentName { get; set; }

    /// <summary>
    /// Maximum sequence length for tokenization (Local embedder only).
    /// Default is 256 for all-MiniLM-L6-v2.
    /// </summary>
    public int MaxSequenceLength { get; set; } = 256;

    /// <summary>
    /// Custom embedder instance. When set, overrides Type property.
    /// Use this to plug in proprietary or specialized embedding models.
    /// </summary>
    public MemPalace.Core.Backends.ICustomEmbedder? CustomEmbedder { get; set; }
}
