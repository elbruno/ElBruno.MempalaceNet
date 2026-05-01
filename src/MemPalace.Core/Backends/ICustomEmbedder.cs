namespace MemPalace.Core.Backends;

/// <summary>
/// Pluggable embedder interface for custom embedding models.
/// Implement this interface to integrate proprietary or specialized embedders
/// without modifying MemPalace.NET source code.
/// 
/// <para>
/// Contract requirements:
/// <list type="bullet">
/// <item><description>ModelIdentity must be unique and stable (used for collection validation)</description></item>
/// <item><description>Dimensions must match actual embedding output (validated on first embed)</description></item>
/// <item><description>EmbedAsync must be thread-safe (concurrent calls allowed)</description></item>
/// <item><description>Embeddings should be normalized unit vectors (for cosine similarity)</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// Example implementation:
/// <code>
/// public class MyCustomEmbedder : ICustomEmbedder
/// {
///     public string ModelIdentity => "my-custom-model-v1";
///     public int Dimensions => 768;
///     
///     public async ValueTask&lt;IReadOnlyList&lt;ReadOnlyMemory&lt;float&gt;&gt;&gt; EmbedAsync(
///         IReadOnlyList&lt;string&gt; texts,
///         CancellationToken ct = default)
///     {
///         // Your embedding logic here (ONNX, API, etc.)
///         // Ensure embeddings are normalized for cosine similarity
///     }
/// }
/// </code>
/// </para>
/// </summary>
public interface ICustomEmbedder : IEmbedder
{
    /// <summary>
    /// Embedder provider name (e.g., "openai", "local", "azureopenai").
    /// Used for factory resolution and diagnostics.
    /// </summary>
    string ProviderName { get; }
    
    /// <summary>
    /// Optional embedder metadata (cost per token, latency SLA, quality metrics, etc.).
    /// Used for MCP tool introspection and monitoring.
    /// </summary>
    IReadOnlyDictionary<string, object> Metadata { get; }
}
