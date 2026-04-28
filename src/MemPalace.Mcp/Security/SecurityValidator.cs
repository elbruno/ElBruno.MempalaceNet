using System.Text.RegularExpressions;

namespace MemPalace.Mcp.Security;

/// <summary>
/// Security validation for MCP write operations.
/// </summary>
public partial class SecurityValidator
{
    private readonly IAuditLogger _auditLogger;
    private static readonly int MaxBatchSize = 100;
    
    [GeneratedRegex(@"^[a-zA-Z0-9_\-\.]+$")]
    private static partial Regex SafeIdentifierRegex();

    public SecurityValidator(IAuditLogger auditLogger)
    {
        _auditLogger = auditLogger;
    }

    /// <summary>
    /// Validates a collection name to prevent SQL injection.
    /// </summary>
    public void ValidateCollectionName(string collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            throw new SecurityException("Collection name cannot be empty");
        }

        if (!SafeIdentifierRegex().IsMatch(collectionName))
        {
            throw new SecurityException($"Collection name '{collectionName}' contains invalid characters. Only alphanumeric, underscore, hyphen, and dot are allowed.");
        }

        if (collectionName.Length > 255)
        {
            throw new SecurityException("Collection name cannot exceed 255 characters");
        }
    }

    /// <summary>
    /// Validates a memory ID.
    /// </summary>
    public void ValidateMemoryId(string memoryId)
    {
        if (string.IsNullOrWhiteSpace(memoryId))
        {
            throw new SecurityException("Memory ID cannot be empty");
        }

        if (memoryId.Length > 512)
        {
            throw new SecurityException("Memory ID cannot exceed 512 characters");
        }
    }

    /// <summary>
    /// Validates batch size for batch operations.
    /// </summary>
    public void ValidateBatchSize(int count)
    {
        if (count <= 0)
        {
            throw new SecurityException("Batch size must be greater than 0");
        }

        if (count > MaxBatchSize)
        {
            throw new SecurityException($"Batch size cannot exceed {MaxBatchSize} items");
        }
    }

    /// <summary>
    /// Validates entity reference format.
    /// </summary>
    public void ValidateEntityRef(string entityRef)
    {
        if (string.IsNullOrWhiteSpace(entityRef))
        {
            throw new SecurityException("Entity reference cannot be empty");
        }

        if (!entityRef.Contains(':'))
        {
            throw new SecurityException($"Entity reference '{entityRef}' must be in format 'type:id'");
        }

        var parts = entityRef.Split(':', 2);
        if (string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            throw new SecurityException($"Entity reference '{entityRef}' has empty type or id");
        }
    }

    /// <summary>
    /// Logs a write operation to the audit log.
    /// </summary>
    public async Task AuditWriteOperationAsync(
        string operation,
        string collection,
        string? memoryId = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken ct = default)
    {
        await _auditLogger.LogAsync(new AuditEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Operation = operation,
            Collection = collection,
            MemoryId = memoryId,
            Metadata = metadata
        }, ct);
    }
}

/// <summary>
/// Exception thrown when a security validation fails.
/// </summary>
public class SecurityException : Exception
{
    public SecurityException(string message) : base(message) { }
}
