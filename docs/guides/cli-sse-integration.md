# CLI SSE Integration Guide

## Overview

As of Phase 2 Workstream A, the MemPalace.NET CLI now supports multiple MCP transports:
- **stdio** (default): Standard input/output for local usage
- **sse**: HTTP/SSE (Server-Sent Events) for web-based clients
- **both**: Experimental mode running both transports simultaneously

## Quick Start

### stdio Transport (Default)

```bash
# Start MCP server with stdio transport (default)
mempalacenet mcp

# Or explicitly specify stdio
mempalacenet mcp --transport stdio
```

**Use case:** Local MCP clients (Claude Desktop, VS Code extensions, MCP Inspector)

### SSE Transport (Web Clients)

```bash
# Start MCP server with HTTP/SSE transport
mempalacenet mcp --transport sse --port 5050

# Custom host and port
mempalacenet mcp --transport sse --host 127.0.0.1 --port 8080
```

**Use case:** Web-based clients, GitHub Copilot CLI, multi-client scenarios

### Both Transports (Experimental)

```bash
# Run stdio and SSE simultaneously
mempalacenet mcp --transport both --port 5050
```

**Use case:** Advanced scenarios requiring both local and web clients

## Architecture

### Transport Selection Flow

```
CLI Start
    ↓
Parse --transport flag
    ↓
    ├─ "stdio" → Add stdio transport only
    ├─ "sse"   → Add HTTP/SSE transport only
    └─ "both"  → Add both transports
    ↓
Build Host with selected transports
    ↓
Start HttpSseTransport if enabled
    ↓
Run Host (stdio or web server)
```

### SSE Transport Implementation

**ServiceCollectionExtensions.cs:**
- `AddMemPalaceMcpWithSse(port, basePath)` - Registers HttpSseTransport
- `HttpSseTransport` starts ASP.NET Core web server
- `SessionManager` handles token-based session authentication

**Endpoints:**
- `POST /mcp` - Client-to-server messages
- `GET /mcp` - Server-to-client SSE stream
- `DELETE /mcp` - Session cleanup

**Headers:**
- `Mcp-Session-Id`: 32-byte session token (created on first POST)
- `Content-Type: text/event-stream` (for SSE responses)

## Examples

### Example 1: stdio for Claude Desktop

```bash
# Add to Claude Desktop MCP config (~/.config/claude/config.json)
{
  "mcp": {
    "servers": {
      "mempalace": {
        "command": "mempalacenet",
        "args": ["mcp"]
      }
    }
  }
}
```

### Example 2: SSE for Web Clients

```bash
# Terminal 1: Start server
mempalacenet mcp --transport sse --port 5050

# Terminal 2: Test with curl
curl -X POST http://127.0.0.1:5050/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"tools/list","id":1}'
```

### Example 3: GitHub Copilot CLI Integration

```bash
# Start SSE transport
mempalacenet mcp --transport sse --port 5050

# Copilot CLI will connect to http://127.0.0.1:5050/mcp
```

## Session Management

### Session Lifecycle

1. **Creation**: Client sends POST without `Mcp-Session-Id` header
2. **Response**: Server returns `Mcp-Session-Id` in response header
3. **Reuse**: Client includes `Mcp-Session-Id` in subsequent requests
4. **Expiration**: Sessions expire after 60 minutes of inactivity
5. **Cleanup**: Client sends DELETE with `Mcp-Session-Id` to clean up

### Session Token Format

- **Length**: 32 bytes (64 hex characters)
- **Generation**: Cryptographically secure random bytes
- **Storage**: In-memory (ephemeral, not persisted)

## Configuration

### Environment Variables

```bash
# Configure SSE host and port
export MEMPALACE_MCP_TRANSPORT=sse
export MEMPALACE_MCP_PORT=5050
export MEMPALACE_MCP_HOST=127.0.0.1
```

### Command-Line Options

```bash
--transport <type>    # stdio, sse, or both (default: stdio)
--port <number>       # HTTP port for SSE (default: 5050)
--host <string>       # HTTP host for SSE (default: 127.0.0.1)
```

## Troubleshooting

### Error: "Transport 'X' is not yet supported"

**Cause:** Invalid transport type specified

**Fix:**
```bash
# Valid transports: stdio, sse, both
mempalacenet mcp --transport sse
```

### Error: "Port already in use"

**Cause:** Another process is using the SSE port

**Fix:**
```bash
# Use a different port
mempalacenet mcp --transport sse --port 5051

# Or find and stop the conflicting process
lsof -i :5050  # macOS/Linux
netstat -ano | findstr :5050  # Windows
```

### SSE Connection Drops

**Cause:** Session timeout or network interruption

**Fix:**
1. Client should implement reconnection logic
2. Check session expiration (60 min default)
3. Verify firewall/proxy settings allow SSE

### stdio Output Garbled

**Cause:** Logging to stdout interferes with MCP protocol

**Fix:** Ensure logging goes to stderr (CLI does this automatically)

## Security Considerations

### Local Only by Default

SSE transport binds to `127.0.0.1` (localhost) by default for security.

**❌ Do not expose to public internet:**
```bash
# Insecure - exposes MCP server to network
mempalacenet mcp --transport sse --host 0.0.0.0
```

**✅ Use localhost for development:**
```bash
# Secure - localhost only
mempalacenet mcp --transport sse --host 127.0.0.1
```

### Session Token Entropy

- Tokens use `System.Security.Cryptography.RandomNumberGenerator`
- 32 bytes = 256 bits of entropy
- Resistant to brute-force attacks

### Future Enhancements (v1.0+)

- TLS/HTTPS support for production deployments
- Client certificate authentication
- API key-based authentication
- Session persistence (Redis, database)

## Performance

### stdio Transport

- **Latency**: <1ms (local process communication)
- **Throughput**: ~10K requests/sec
- **Overhead**: Minimal (pipes, no network)

### SSE Transport

- **Latency**: ~5-10ms (localhost HTTP)
- **Throughput**: ~1K requests/sec (HTTP overhead)
- **Overhead**: ASP.NET Core web server

### Scalability

- stdio: Single client only
- SSE: Multiple clients (limited by server resources)
- both: stdio + SSE clients simultaneously

## Migration Guide

### From stdio-Only to SSE

**Before (Phase 1):**
```bash
mempalacenet mcp  # stdio only
```

**After (Phase 2):**
```bash
mempalacenet mcp --transport sse --port 5050
```

**Backward Compatibility:**
- stdio remains the default (no breaking changes)
- Existing MCP configs work unchanged
- Add `--transport sse` only when needed

## See Also

- [MCP Protocol Specification](https://spec.modelcontextprotocol.io/)
- [Session Manager Implementation](../../src/MemPalace.Mcp/Transports/SessionManager.cs)
- [HttpSseTransport Implementation](../../src/MemPalace.Mcp/Transports/HttpSseTransport.cs)
- [CLI Command Reference](../cli.md)
