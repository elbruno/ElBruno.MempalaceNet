using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace MemPalace.Mcp.Transports;

/// <summary>
/// Manages HTTP/SSE sessions with crypto-secure tokens and expiration.
/// </summary>
public sealed class SessionManager : IDisposable
{
    private readonly ConcurrentDictionary<string, Session> _sessions = new();
    private readonly TimeSpan _sessionTimeout;
    private readonly Timer _cleanupTimer;
    private bool _disposed;

    public SessionManager(TimeSpan? sessionTimeout = null)
    {
        _sessionTimeout = sessionTimeout ?? TimeSpan.FromMinutes(60);
        _cleanupTimer = new Timer(CleanupExpiredSessions, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Creates a new session with a crypto-secure 32-byte token.
    /// </summary>
    public string CreateSession()
    {
        var sessionId = GenerateSessionId();
        var session = new Session(sessionId, DateTimeOffset.UtcNow);
        _sessions[sessionId] = session;
        return sessionId;
    }

    /// <summary>
    /// Validates a session token and updates its last activity timestamp.
    /// </summary>
    /// <param name="sessionId">Session ID to validate</param>
    /// <returns>True if session is valid and not expired, false otherwise</returns>
    public bool ValidateSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return false;

        if (!_sessions.TryGetValue(sessionId, out var session))
            return false;

        // Check expiration
        if (DateTimeOffset.UtcNow - session.LastActivity > _sessionTimeout)
        {
            _sessions.TryRemove(sessionId, out _);
            return false;
        }

        // Update last activity
        session.UpdateActivity();
        return true;
    }

    /// <summary>
    /// Removes a session.
    /// </summary>
    public void RemoveSession(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
    }

    /// <summary>
    /// Gets the number of active sessions.
    /// </summary>
    public int ActiveSessionCount => _sessions.Count;

    /// <summary>
    /// Generates a crypto-secure 32-byte session ID (base64-encoded).
    /// </summary>
    private static string GenerateSessionId()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('='); // URL-safe base64
    }

    private void CleanupExpiredSessions(object? state)
    {
        var now = DateTimeOffset.UtcNow;
        var expiredSessions = _sessions
            .Where(kvp => now - kvp.Value.LastActivity > _sessionTimeout)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var sessionId in expiredSessions)
        {
            _sessions.TryRemove(sessionId, out _);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        var waitHandle = new ManualResetEvent(false);
        try
        {
            _cleanupTimer.Dispose(waitHandle);
            waitHandle.WaitOne();
        }
        finally
        {
            waitHandle.Dispose();
        }
        _sessions.Clear();
        _disposed = true;
    }

    private sealed class Session
    {
        public string Id { get; }
        public DateTimeOffset CreatedAt { get; }
        public DateTimeOffset LastActivity { get; private set; }

        public Session(string id, DateTimeOffset createdAt)
        {
            Id = id;
            CreatedAt = createdAt;
            LastActivity = createdAt;
        }

        public void UpdateActivity()
        {
            LastActivity = DateTimeOffset.UtcNow;
        }
    }
}
