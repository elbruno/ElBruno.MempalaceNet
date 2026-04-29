using System.Diagnostics;

namespace MemPalace.Ai;

/// <summary>
/// Health check implementation for Ollama embedder services.
/// Verifies Ollama availability by querying the /api/tags endpoint.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies that Ollama is running and responsive by making
/// a lightweight HTTP GET request to the /api/tags endpoint, which lists available models.
/// This is faster than attempting an actual embedding operation and doesn't require
/// a specific model to be loaded.
/// </para>
/// <para>
/// The health check is designed to complete quickly (typically &lt;100ms for healthy services)
/// and respects cancellation tokens for timeout enforcement.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic health check with 100ms timeout
/// var healthCheck = new OllamaHealthCheck("http://localhost:11434");
/// using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
/// var status = await healthCheck.CheckHealthAsync(cts.Token);
/// 
/// if (status.IsHealthy)
/// {
///     Console.WriteLine($"Ollama responded in {status.ResponseTime.TotalMilliseconds}ms");
/// }
/// else
/// {
///     Console.WriteLine($"Ollama unhealthy: {status.ErrorMessage}");
/// }
/// </code>
/// <code>
/// // Using with custom HttpClient for connection pooling
/// var httpClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(500) };
/// var healthCheck = new OllamaHealthCheck("http://localhost:11434", httpClient);
/// var status = await healthCheck.CheckHealthAsync();
/// </code>
/// </example>
public sealed class OllamaHealthCheck : IEmbedderHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly bool _disposeClient;

    /// <summary>
    /// Creates a new Ollama health check with a default HttpClient.
    /// </summary>
    /// <param name="baseUrl">
    /// Ollama base URL (e.g., "http://localhost:11434").
    /// Default: "http://localhost:11434"
    /// </param>
    /// <remarks>
    /// When using this constructor, the HttpClient is created internally and will be disposed
    /// when this health check instance is disposed.
    /// </remarks>
    public OllamaHealthCheck(string baseUrl = "http://localhost:11434")
    {
        _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        _httpClient = new HttpClient();
        _disposeClient = true;
    }

    /// <summary>
    /// Creates a new Ollama health check with a custom HttpClient.
    /// </summary>
    /// <param name="baseUrl">
    /// Ollama base URL (e.g., "http://localhost:11434").
    /// </param>
    /// <param name="httpClient">
    /// Custom HttpClient to use for health check requests.
    /// Caller is responsible for HttpClient lifecycle management.
    /// </param>
    /// <remarks>
    /// Use this constructor when you want to control HttpClient settings
    /// (timeouts, connection pooling, handlers) or share an HttpClient instance
    /// across multiple health checks.
    /// </remarks>
    public OllamaHealthCheck(string baseUrl, HttpClient httpClient)
    {
        _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _disposeClient = false;
    }

    /// <summary>
    /// Checks Ollama health by querying the /api/tags endpoint.
    /// </summary>
    /// <param name="ct">
    /// Cancellation token for timeout enforcement.
    /// Recommended: 100-500ms timeout for production use.
    /// </param>
    /// <returns>
    /// Health status indicating availability, response time, and any errors.
    /// </returns>
    /// <remarks>
    /// The health check makes a GET request to {baseUrl}/api/tags.
    /// A successful 200 OK response indicates Ollama is healthy.
    /// Common failure scenarios:
    /// <list type="bullet">
    /// <item><description>Connection refused (Ollama not running)</description></item>
    /// <item><description>Timeout (Ollama unresponsive or network issues)</description></item>
    /// <item><description>HTTP error responses (server errors)</description></item>
    /// </list>
    /// </remarks>
    public async Task<EmbedderHealthStatus> CheckHealthAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var endpoint = $"{_baseUrl.TrimEnd('/')}/api/tags";
            var response = await _httpClient.GetAsync(endpoint, ct);
            
            stopwatch.Stop();
            
            if (response.IsSuccessStatusCode)
            {
                return EmbedderHealthStatus.Healthy(stopwatch.Elapsed);
            }
            
            return EmbedderHealthStatus.Unhealthy(
                stopwatch.Elapsed,
                $"Ollama returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            stopwatch.Stop();
            return EmbedderHealthStatus.Unhealthy(
                stopwatch.Elapsed,
                "Health check timed out");
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            return EmbedderHealthStatus.Unhealthy(
                stopwatch.Elapsed,
                $"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return EmbedderHealthStatus.Unhealthy(
                stopwatch.Elapsed,
                $"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Disposes the internal HttpClient if it was created by this instance.
    /// </summary>
    public void Dispose()
    {
        if (_disposeClient)
        {
            _httpClient.Dispose();
        }
    }
}
