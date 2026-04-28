namespace MemPalace.Mcp.Transports;

/// <summary>
/// Abstraction for MCP transport layers (stdio, HTTP/SSE).
/// </summary>
public interface IMcpTransport
{
    /// <summary>
    /// Gets the transport type identifier.
    /// </summary>
    string TransportType { get; }

    /// <summary>
    /// Starts the transport layer and begins listening for messages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the transport layer and cleans up resources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to the client(s).
    /// </summary>
    /// <param name="message">JSON-RPC message to send</param>
    /// <param name="sessionId">Optional session ID for SSE transport</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendMessageAsync(string message, string? sessionId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a message is received from a client.
    /// </summary>
    event EventHandler<MessageReceivedEventArgs>? MessageReceived;
}

/// <summary>
/// Event args for MessageReceived event.
/// </summary>
public class MessageReceivedEventArgs : EventArgs
{
    public string Message { get; }
    public string? SessionId { get; }

    public MessageReceivedEventArgs(string message, string? sessionId = null)
    {
        Message = message;
        SessionId = sessionId;
    }
}
