using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MemPalace.Ai;

/// <summary>
/// Health check implementation for OpenAI embedder services.
/// Verifies OpenAI API availability by querying the /v1/models endpoint.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies that the OpenAI API is accessible and the provided
/// API key is valid by making a lightweight HTTP GET request to the /v1/models endpoint.
/// This is faster than attempting an actual embedding operation and uses minimal API quota.
/// </para>
/// <para>
/// The health check is designed to complete quickly (typically &lt;200ms for healthy services)
/// and respects cancellation tokens for timeout enforcement.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic health check with API key
/// var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
/// var healthCheck = new OpenAIHealthCheck(apiKey);
/// using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
/// var status = await healthCheck.CheckHealthAsync(cts.Token);
/// 
/// if (status.IsHealthy)
/// {
///     Console.WriteLine($"OpenAI API responded in {status.ResponseTime.TotalMilliseconds}ms");
/// }
/// else
/// {
///     Console.WriteLine($"OpenAI API unhealthy: {status.ErrorMessage}");
/// }
/// </code>
/// <code>
/// // Azure OpenAI health check
/// var azureHealthCheck = new OpenAIHealthCheck(
///     apiKey: Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY"),
///     endpoint: "https://my-resource.openai.azure.com");
/// var status = await azureHealthCheck.CheckHealthAsync();
/// </code>
/// </example>
public sealed class OpenAIHealthCheck : IEmbedderHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _endpoint;
    private readonly bool _disposeClient;
    private readonly bool _isAzure;

    /// <summary>
    /// Creates a new OpenAI health check for the standard OpenAI API.
    /// </summary>
    /// <param name="apiKey">
    /// OpenAI API key. Required for authentication.
    /// </param>
    /// <param name="endpoint">
    /// OpenAI API endpoint. Default: "https://api.openai.com/v1"
    /// For Azure OpenAI, provide your Azure endpoint (e.g., "https://my-resource.openai.azure.com").
    /// </param>
    /// <remarks>
    /// When using this constructor, the HttpClient is created internally and will be disposed
    /// when this health check instance is disposed.
    /// </remarks>
    public OpenAIHealthCheck(string apiKey, string endpoint = "https://api.openai.com/v1")
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _httpClient = new HttpClient();
        _disposeClient = true;
        _isAzure = endpoint.Contains(".azure.com", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates a new OpenAI health check with a custom HttpClient.
    /// </summary>
    /// <param name="apiKey">
    /// OpenAI API key. Required for authentication.
    /// </param>
    /// <param name="httpClient">
    /// Custom HttpClient to use for health check requests.
    /// Caller is responsible for HttpClient lifecycle management.
    /// </param>
    /// <param name="endpoint">
    /// OpenAI API endpoint. Default: "https://api.openai.com/v1"
    /// </param>
    /// <remarks>
    /// Use this constructor when you want to control HttpClient settings
    /// (timeouts, connection pooling, handlers) or share an HttpClient instance
    /// across multiple health checks.
    /// </remarks>
    public OpenAIHealthCheck(string apiKey, HttpClient httpClient, string endpoint = "https://api.openai.com/v1")
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _disposeClient = false;
        _isAzure = endpoint.Contains(".azure.com", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks OpenAI API health by querying the models endpoint or making a minimal embedding request.
    /// </summary>
    /// <param name="ct">
    /// Cancellation token for timeout enforcement.
    /// Recommended: 200-1000ms timeout for production use (API calls may be slower than local services).
    /// </param>
    /// <returns>
    /// Health status indicating availability, response time, and any errors.
    /// </returns>
    /// <remarks>
    /// For standard OpenAI, the health check queries GET /v1/models.
    /// For Azure OpenAI, it makes a minimal embedding request since Azure doesn't expose a models list endpoint.
    /// A successful response indicates the API is healthy and the API key is valid.
    /// Common failure scenarios:
    /// <list type="bullet">
    /// <item><description>Invalid API key (401 Unauthorized)</description></item>
    /// <item><description>Network issues or timeout</description></item>
    /// <item><description>API rate limits (429 Too Many Requests)</description></item>
    /// <item><description>Server errors (5xx)</description></item>
    /// </list>
    /// </remarks>
    public async Task<EmbedderHealthStatus> CheckHealthAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            if (_isAzure)
            {
                // Azure OpenAI doesn't have a simple models list endpoint
                // Use a minimal embedding request as a health check
                return await CheckAzureHealthAsync(stopwatch, ct);
            }
            
            // Standard OpenAI: check /v1/models endpoint
            var endpoint = $"{_endpoint.TrimEnd('/')}/models";
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            
            var response = await _httpClient.SendAsync(request, ct);
            stopwatch.Stop();
            
            if (response.IsSuccessStatusCode)
            {
                return EmbedderHealthStatus.Healthy(stopwatch.Elapsed);
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return EmbedderHealthStatus.Unhealthy(
                    stopwatch.Elapsed,
                    "Invalid API key");
            }
            
            return EmbedderHealthStatus.Unhealthy(
                stopwatch.Elapsed,
                $"OpenAI API returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
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

    private async Task<EmbedderHealthStatus> CheckAzureHealthAsync(Stopwatch stopwatch, CancellationToken ct)
    {
        try
        {
            // For Azure, we need to make a minimal embedding request
            // Use a very short test string to minimize API costs
            var endpoint = $"{_endpoint.TrimEnd('/')}/openai/deployments/text-embedding-ada-002/embeddings?api-version=2024-02-01";
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("api-key", _apiKey);
            
            var payload = new
            {
                input = "test"
            };
            
            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.SendAsync(request, ct);
            stopwatch.Stop();
            
            if (response.IsSuccessStatusCode)
            {
                return EmbedderHealthStatus.Healthy(stopwatch.Elapsed);
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return EmbedderHealthStatus.Unhealthy(
                    stopwatch.Elapsed,
                    "Invalid API key");
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return EmbedderHealthStatus.Unhealthy(
                    stopwatch.Elapsed,
                    "Azure OpenAI deployment not found (verify deployment name and endpoint)");
            }
            
            return EmbedderHealthStatus.Unhealthy(
                stopwatch.Elapsed,
                $"Azure OpenAI API returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return EmbedderHealthStatus.Unhealthy(
                stopwatch.Elapsed,
                $"Azure health check error: {ex.Message}");
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
