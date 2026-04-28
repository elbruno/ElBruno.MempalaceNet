namespace MemPalace.Mcp.Security;

/// <summary>
/// Interface for audit logging of write operations.
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Logs an audit entry.
    /// </summary>
    Task LogAsync(AuditEntry entry, CancellationToken ct = default);
}

/// <summary>
/// Audit log entry.
/// </summary>
public record AuditEntry
{
    public required DateTimeOffset Timestamp { get; init; }
    public required string Operation { get; init; }
    public required string Collection { get; init; }
    public string? MemoryId { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}
