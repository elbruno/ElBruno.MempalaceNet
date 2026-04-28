# MCP Security

This document describes the security validations and audit logging implemented for MCP write operations in MemPalace.NET.

## Overview

Write operations in the MCP server are protected by multiple security layers:

1. **Input Validation** - All user inputs are validated to prevent injection attacks
2. **Confirmation Prompts** - Destructive operations require user confirmation
3. **Rate Limiting** - Batch operations are limited to prevent abuse
4. **Audit Logging** - All write operations are logged for accountability

## Security Validations

### Collection Name Validation

Collection names must:
- Not be empty or whitespace
- Contain only alphanumeric characters, underscore, hyphen, and dot (`a-zA-Z0-9_-.`)
- Not exceed 255 characters

**Example valid names:**
- `my-collection`
- `user_data`
- `collection.v2`

**Example invalid names:**
- `collection; DROP TABLE` (SQL injection attempt)
- `collection@special` (special characters)
- Empty or null strings

### Memory ID Validation

Memory IDs must:
- Not be empty or whitespace
- Not exceed 512 characters

### Batch Size Validation

Batch operations (`palace_batch_store`) are limited to:
- Minimum: 1 item
- Maximum: 100 items per batch

This prevents resource exhaustion attacks.

### Entity Reference Validation

Knowledge graph entity references must:
- Not be empty or whitespace
- Follow the format `type:id` (e.g., `person:alice`, `project:mempalace`)
- Have non-empty type and id parts

## Confirmation Prompts

Destructive operations require user confirmation:

### `palace_delete`
Deletes a single memory. Requires confirmation with:
- Operation: `"delete memory"`
- Target: `"{collection}/{id}"`

### `palace_delete_collection`
Deletes an entire collection. Requires confirmation with:
- Operation: `"delete collection"`
- Target: `"{collection}"`

If the user declines confirmation, the operation returns status `"cancelled"` and no changes are made.

## Audit Logging

All write operations are logged to `~/.palace/audit.log` in JSON format.

### Log Entry Format

```json
{
  "Timestamp": "2024-04-28T10:30:00.123Z",
  "Operation": "palace_store",
  "Collection": "my-collection",
  "MemoryId": "abc123",
  "Metadata": {
    "key": "value"
  }
}
```

### Logged Operations

- `palace_store` - Single memory stored
- `palace_update` - Memory updated
- `palace_delete` - Memory deleted
- `palace_batch_store` - Batch of memories stored (includes count in metadata)
- `palace_create_collection` - Collection created
- `palace_delete_collection` - Collection deleted
- `kg_add_entity` - Knowledge graph entity added
- `kg_add_relationship` - Knowledge graph relationship added

### Log File Location

- **Windows:** `C:\Users\{username}\.palace\audit.log`
- **macOS/Linux:** `~/.palace/audit.log`

The log directory is created automatically if it doesn't exist.

## Error Handling

Security validation failures throw `SecurityException` with descriptive messages:

```csharp
try
{
    await writeTools.PalaceStore(content, "my-collection");
}
catch (SecurityException ex)
{
    // Handle validation failure
    Console.Error.WriteLine($"Security validation failed: {ex.Message}");
}
```

## Testing

Security validations are tested in:
- `SecurityValidatorTests.cs` - Unit tests for validation logic
- `WriteOperationsTests.cs` - Integration tests for write operations
- `KnowledgeGraphWriteToolsTests.cs` - Knowledge graph write operation tests

All tests verify:
- Valid inputs are accepted
- Invalid inputs are rejected with appropriate exceptions
- Confirmation prompts are called for destructive operations
- Audit logs are written correctly

## Customization

### Custom Audit Logger

Implement `IAuditLogger` to customize audit logging:

```csharp
public class CustomAuditLogger : IAuditLogger
{
    public async Task LogAsync(AuditEntry entry, CancellationToken ct = default)
    {
        // Custom logging implementation (e.g., database, cloud service)
    }
}

// Register in DI
services.AddSingleton<IAuditLogger, CustomAuditLogger>();
```

### Custom Confirmation Prompt

Implement `IConfirmationPrompt` to customize confirmation UI:

```csharp
public class CustomConfirmationPrompt : IConfirmationPrompt
{
    public async Task<bool> ConfirmAsync(string operation, string target, CancellationToken ct = default)
    {
        // Custom confirmation logic (e.g., MCP client integration)
        return await ShowConfirmationDialogAsync(operation, target);
    }
}

// Register in DI
services.AddSingleton<IConfirmationPrompt, CustomConfirmationPrompt>();
```

## Best Practices

1. **Always validate inputs** - Never trust user inputs, even from authenticated clients
2. **Log all write operations** - Maintain an audit trail for accountability
3. **Require confirmation for destructive operations** - Prevent accidental data loss
4. **Limit batch sizes** - Prevent resource exhaustion
5. **Monitor audit logs** - Regularly review logs for suspicious activity
6. **Use safe identifiers** - Avoid special characters in collection names and IDs

## Security Considerations

### SQL Injection Prevention

The `SecurityValidator` uses regex-based validation to ensure collection names contain only safe characters. This prevents SQL injection attacks like:

```
collection'; DROP TABLE collections; --
```

### Rate Limiting

Batch operations are limited to 100 items to prevent:
- Resource exhaustion
- Memory overflow
- Denial of service attacks

### Session Isolation

While not fully implemented in v0.5, the security framework is designed to support:
- Session token validation
- Cross-session write prevention
- User-based access control

These features will be added in future releases.

## Future Enhancements

Planned security enhancements include:

- [ ] Session-based access control
- [ ] Role-based permissions (read-only, read-write, admin)
- [ ] Encrypted audit logs
- [ ] Rate limiting per user/session
- [ ] Anomaly detection (unusual batch sizes, rapid writes)
- [ ] MCP client-integrated confirmation UI

## References

- [MCP Protocol Specification](https://modelcontextprotocol.io/)
- [MemPalace.NET Documentation](../README.md)
- [MCP Tool Catalog](./mcp-tool-catalog.md)
