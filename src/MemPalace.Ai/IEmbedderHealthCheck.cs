namespace MemPalace.Ai;

/// <summary>
/// Provides health checking capabilities for embedding services.
/// Implementations can verify that an embedder backend (Ollama, OpenAI, etc.) is responsive
/// and available before attempting embedding operations.
/// </summary>
/// <remarks>
/// <para>
/// Health checks are particularly useful for:
/// <list type="bullet">
/// <item><description>Fast-fail detection when embedder services are down or unreachable</description></item>
/// <item><description>Graceful degradation in applications that can tolerate embedder unavailability</description></item>
/// <item><description>Monitoring and alerting infrastructure for production deployments</description></item>
/// <item><description>Integration tests that need to verify embedder availability before running</description></item>
/// </list>
/// </para>
/// <para>
/// Typical implementations should use short timeouts (e.g., 100ms) to quickly detect service issues
/// without blocking application startup or request processing.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Check Ollama health before creating an embedder
/// var healthCheck = new OllamaHealthCheck("http://localhost:11434");
/// var status = await healthCheck.CheckHealthAsync(
///     new CancellationTokenSource(TimeSpan.FromMilliseconds(100)).Token);
/// 
/// if (status.IsHealthy)
/// {
///     Console.WriteLine($"Ollama is healthy (response time: {status.ResponseTime.TotalMilliseconds}ms)");
///     // Proceed with embedder initialization
/// }
/// else
/// {
///     Console.WriteLine($"Ollama is unavailable: {status.ErrorMessage}");
///     // Fall back to alternative embedder or handle gracefully
/// }
/// </code>
/// </example>
public interface IEmbedderHealthCheck
{
    /// <summary>
    /// Checks the health and availability of the embedder service.
    /// </summary>
    /// <param name="ct">
    /// Cancellation token to control health check timeout.
    /// Recommended: 100-500ms timeout for fast detection of service issues.
    /// </param>
    /// <returns>
    /// A task that resolves to an <see cref="EmbedderHealthStatus"/> indicating
    /// whether the service is healthy, response time, and any error details.
    /// </returns>
    /// <remarks>
    /// This method should perform a minimal check against the embedder service
    /// (e.g., a lightweight API call) rather than a full embedding operation.
    /// The check should respect the cancellation token and complete quickly.
    /// </remarks>
    Task<EmbedderHealthStatus> CheckHealthAsync(CancellationToken ct = default);
}

/// <summary>
/// Represents the health status of an embedder service, including availability,
/// response time, and error information if applicable.
/// </summary>
public sealed class EmbedderHealthStatus
{
    /// <summary>
    /// Indicates whether the embedder service is healthy and available.
    /// </summary>
    /// <remarks>
    /// A service is considered healthy if it responds successfully within the timeout period.
    /// Network errors, timeouts, and service errors all result in IsHealthy = false.
    /// </remarks>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// The time taken to complete the health check.
    /// </summary>
    /// <remarks>
    /// For successful checks, this indicates actual service response time.
    /// For failed or timed-out checks, this may represent the timeout duration.
    /// </remarks>
    public TimeSpan ResponseTime { get; set; }

    /// <summary>
    /// Error message describing why the health check failed, if applicable.
    /// Null when IsHealthy is true.
    /// </summary>
    /// <remarks>
    /// Common error scenarios:
    /// <list type="bullet">
    /// <item><description>Network errors (connection refused, DNS failures)</description></item>
    /// <item><description>Timeout exceeded</description></item>
    /// <item><description>HTTP error responses (4xx, 5xx)</description></item>
    /// <item><description>Service-specific errors (model not loaded, API key invalid)</description></item>
    /// </list>
    /// </remarks>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a healthy status result.
    /// </summary>
    public static EmbedderHealthStatus Healthy(TimeSpan responseTime) => new()
    {
        IsHealthy = true,
        ResponseTime = responseTime,
        ErrorMessage = null
    };

    /// <summary>
    /// Creates an unhealthy status result with an error message.
    /// </summary>
    public static EmbedderHealthStatus Unhealthy(TimeSpan responseTime, string errorMessage) => new()
    {
        IsHealthy = false,
        ResponseTime = responseTime,
        ErrorMessage = errorMessage
    };
}
