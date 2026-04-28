using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace MemPalace.Mcp.Transports;

/// <summary>
/// ASP.NET Core HTTP/SSE transport implementation for MCP protocol.
/// Implements the MCP HTTP/SSE specification with session management.
/// </summary>
public sealed class HttpSseTransport : IMcpTransport, IDisposable
{
    private readonly ILogger<HttpSseTransport> _logger;
    private readonly SessionManager _sessionManager;
    private readonly ConcurrentDictionary<string, SseConnection> _connections = new();
    private readonly string _basePath;
    private readonly int _port;
    private WebApplication? _app;
    private bool _disposed;

    public string TransportType => "sse";

    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    public HttpSseTransport(
        ILogger<HttpSseTransport> logger,
        SessionManager? sessionManager = null,
        string basePath = "/mcp",
        int port = 5050)
    {
        _logger = logger;
        _sessionManager = sessionManager ?? new SessionManager();
        _basePath = basePath;
        _port = port;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{_port}");
        
        _app = builder.Build();
        
        // POST /mcp - Client to server messages
        _app.MapPost(_basePath, HandlePostAsync);
        
        // GET /mcp - Server to client SSE stream
        _app.MapGet(_basePath, HandleGetAsync);
        
        // DELETE /mcp - Session cleanup
        _app.MapDelete(_basePath, HandleDeleteAsync);

        _logger.LogInformation("Starting HTTP/SSE transport on http://127.0.0.1:{Port}{Path}", _port, _basePath);
        
        return _app.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_app != null)
        {
            await _app.StopAsync(cancellationToken);
            await _app.DisposeAsync();
            _app = null;
        }

        // Close all SSE connections
        foreach (var connection in _connections.Values)
        {
            await connection.CloseAsync();
        }
        _connections.Clear();

        _logger.LogInformation("HTTP/SSE transport stopped");
    }

    public async Task SendMessageAsync(string message, string? sessionId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            _logger.LogWarning("SendMessageAsync called without sessionId");
            return;
        }

        if (!_connections.TryGetValue(sessionId, out var connection))
        {
            _logger.LogWarning("No active SSE connection for session {SessionId}", sessionId);
            return;
        }

        await connection.SendEventAsync(message, cancellationToken);
    }

    private async Task HandlePostAsync(HttpContext context)
    {
        // Validate session
        var sessionId = context.Request.Headers["Mcp-Session-Id"].ToString();
        
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            // Create new session
            sessionId = _sessionManager.CreateSession();
            context.Response.Headers["Mcp-Session-Id"] = sessionId;
            _logger.LogInformation("Created new session {SessionId}", sessionId);
        }
        else if (!_sessionManager.ValidateSession(sessionId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired session" });
            return;
        }

        // Read message body
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
        var message = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(message))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "Empty message body" });
            return;
        }

        _logger.LogDebug("Received message from session {SessionId}: {MessageLength} bytes", sessionId, message.Length);

        // Raise MessageReceived event
        MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message, sessionId));

        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsJsonAsync(new { status = "received" });
    }

    private async Task HandleGetAsync(HttpContext context)
    {
        // Validate session
        var sessionId = context.Request.Headers["Mcp-Session-Id"].ToString();
        
        if (string.IsNullOrWhiteSpace(sessionId) || !_sessionManager.ValidateSession(sessionId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired session" });
            return;
        }

        // Set SSE headers
        context.Response.Headers["Content-Type"] = "text/event-stream";
        context.Response.Headers["Cache-Control"] = "no-cache";
        context.Response.Headers["Connection"] = "keep-alive";

        _logger.LogInformation("SSE connection established for session {SessionId}", sessionId);

        // Create SSE connection
        var connection = new SseConnection(context.Response.Body, sessionId, _logger);
        _connections[sessionId] = connection;

        try
        {
            // Send initial connection event
            await connection.SendEventAsync("{\"type\":\"connection_established\"}", context.RequestAborted);

            // Keep connection alive until cancelled
            await Task.Delay(Timeout.Infinite, context.RequestAborted);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SSE connection closed for session {SessionId}", sessionId);
        }
        finally
        {
            _connections.TryRemove(sessionId, out _);
            await connection.CloseAsync();
        }
    }

    private async Task HandleDeleteAsync(HttpContext context)
    {
        var sessionId = context.Request.Headers["Mcp-Session-Id"].ToString();
        
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "Missing session ID" });
            return;
        }

        _sessionManager.RemoveSession(sessionId);
        
        if (_connections.TryRemove(sessionId, out var connection))
        {
            await connection.CloseAsync();
        }

        _logger.LogInformation("Session {SessionId} deleted", sessionId);
        
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsJsonAsync(new { status = "deleted" });
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _app?.DisposeAsync().AsTask().Wait();
        _sessionManager.Dispose();
        
        foreach (var connection in _connections.Values)
        {
            connection.CloseAsync().Wait();
        }
        _connections.Clear();

        _disposed = true;
    }

    private sealed class SseConnection
    {
        private readonly Stream _stream;
        private readonly string _sessionId;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _writeLock = new(1, 1);
        private int _eventId;

        public SseConnection(Stream stream, string sessionId, ILogger logger)
        {
            _stream = stream;
            _sessionId = sessionId;
            _logger = logger;
        }

        public async Task SendEventAsync(string data, CancellationToken cancellationToken = default)
        {
            await _writeLock.WaitAsync(cancellationToken);
            try
            {
                var eventId = Interlocked.Increment(ref _eventId);
                var sseData = $"id: {eventId}\ndata: {data}\n\n";
                var bytes = Encoding.UTF8.GetBytes(sseData);
                
                await _stream.WriteAsync(bytes, cancellationToken);
                await _stream.FlushAsync(cancellationToken);
                
                _logger.LogTrace("Sent SSE event {EventId} to session {SessionId}", eventId, _sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send SSE event to session {SessionId}", _sessionId);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public async Task CloseAsync()
        {
            await _writeLock.WaitAsync();
            try
            {
                _stream.Close();
            }
            finally
            {
                _writeLock.Release();
                _writeLock.Dispose();
            }
        }
    }
}
