# MCP SSE Transport Setup Guide

## Overview

MemPalace.NET MCP server supports two transport layers:

1. **stdio** (default): Standard input/output for desktop integrations (Claude Desktop, VS Code)
2. **HTTP/SSE**: Server-Sent Events over HTTP for web-based integrations (Copilot CLI, browser assistants)

This guide covers the HTTP/SSE transport layer implementation.

---

## Architecture

### Components

1. **IMcpTransport**: Abstract transport interface for stdio and HTTP/SSE
2. **SessionManager**: Manages HTTP sessions with crypto-secure tokens (32-byte) and 60-minute expiry
3. **HttpSseTransport**: ASP.NET Core implementation of HTTP/SSE protocol
4. **SseConnection**: Manages individual SSE stream connections

### Protocol Flow

```
Client                          Server
  |                               |
  | POST /mcp (no session)        |
  |------------------------------>|
  |                               | Create session
  |<------------------------------| 
  |    200 OK + Mcp-Session-Id    |
  |                               |
  | GET /mcp (with session)       |
  |------------------------------>|
  |                               | Validate session
  |<------------------------------| 
  |    SSE stream (text/event-stream)
  |                               |
  | POST /mcp (JSON-RPC message)  |
  |------------------------------>|
  |                               | Process message
  |<------------------------------| Server sends response via SSE
  |    200 OK                     |
  |                               |
  | DELETE /mcp (session cleanup) |
  |------------------------------>|
  |    200 OK                     |
```

---

## HTTP Endpoints

### POST /mcp

**Purpose:** Send JSON-RPC messages from client to server

**Request Headers:**
- `Mcp-Session-Id` (optional for first request): Session identifier
- `Content-Type: application/json`

**Request Body:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "palace_search",
  "params": {
    "query": "authentication patterns",
    "collection": "docs"
  }
}
```

**Response (first request):**
```http
HTTP/1.1 200 OK
Mcp-Session-Id: <32-byte-crypto-secure-token>
Content-Type: application/json

{
  "status": "received"
}
```

**Response (subsequent requests):**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "status": "received"
}
```

**Error Responses:**
- `401 Unauthorized`: Invalid or expired session
- `400 Bad Request`: Empty message body

---

### GET /mcp

**Purpose:** Establish SSE stream for server-to-client messages

**Request Headers:**
- `Mcp-Session-Id` (required): Valid session identifier

**Response (success):**
```http
HTTP/1.1 200 OK
Content-Type: text/event-stream
Cache-Control: no-cache
Connection: keep-alive

id: 1
data: {"type":"connection_established"}

id: 2
data: {"jsonrpc":"2.0","id":1,"result":{"hits":[...]}}

```

**Error Responses:**
- `401 Unauthorized`: Invalid or expired session

---

### DELETE /mcp

**Purpose:** Terminate session and clean up resources

**Request Headers:**
- `Mcp-Session-Id` (required): Session to delete

**Response:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "status": "deleted"
}
```

**Error Responses:**
- `400 Bad Request`: Missing session ID

---

## Session Management

### Session Creation

- **Trigger:** First POST /mcp without `Mcp-Session-Id` header
- **Token:** 32-byte crypto-secure random token (RandomNumberGenerator.Fill)
- **Encoding:** URL-safe base64 (no `+`, `/`, `=` padding)
- **Length:** 42-44 characters

### Session Validation

- **Timeout:** 60 minutes (configurable)
- **Activity Updates:** Each POST /mcp extends session lifetime
- **Cleanup:** Background timer removes expired sessions every 5 minutes

### Session Storage

- **Backend:** ConcurrentDictionary (thread-safe)
- **Metadata:** Session ID, created timestamp, last activity timestamp

---

## Usage Example

### C# Client

```csharp
using System.Net.Http;
using System.Text;
using System.Text.Json;

var client = new HttpClient();
string? sessionId = null;

// 1. Create session
var content = new StringContent(
    JsonSerializer.Serialize(new { jsonrpc = "2.0", method = "ping" }),
    Encoding.UTF8,
    "application/json"
);
var response = await client.PostAsync("http://127.0.0.1:5050/mcp", content);
sessionId = response.Headers.GetValues("Mcp-Session-Id").First();

// 2. Open SSE connection
var sseRequest = new HttpRequestMessage(HttpMethod.Get, "http://127.0.0.1:5050/mcp");
sseRequest.Headers.Add("Mcp-Session-Id", sessionId);
var sseResponse = await client.SendAsync(sseRequest, HttpCompletionOption.ResponseHeadersRead);
var sseStream = await sseResponse.Content.ReadAsStreamAsync();

// 3. Send JSON-RPC message
var searchContent = new StringContent(
    JsonSerializer.Serialize(new
    {
        jsonrpc = "2.0",
        id = 1,
        method = "palace_search",
        @params = new { query = "test", collection = "default" }
    }),
    Encoding.UTF8,
    "application/json"
);
var searchRequest = new HttpRequestMessage(HttpMethod.Post, "http://127.0.0.1:5050/mcp")
{
    Content = searchContent
};
searchRequest.Headers.Add("Mcp-Session-Id", sessionId);
await client.SendAsync(searchRequest);

// 4. Read SSE events
using var reader = new StreamReader(sseStream);
while (!reader.EndOfStream)
{
    var line = await reader.ReadLineAsync();
    if (line?.StartsWith("data: ") == true)
    {
        var data = line.Substring(6);
        Console.WriteLine($"Event: {data}");
    }
}

// 5. Cleanup
var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "http://127.0.0.1:5050/mcp");
deleteRequest.Headers.Add("Mcp-Session-Id", sessionId);
await client.SendAsync(deleteRequest);
```

---

## Configuration

### ASP.NET Core Integration

```csharp
using MemPalace.Mcp.Transports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Configure services
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<HttpSseTransport>>();

// Create transport
var sessionManager = new SessionManager(TimeSpan.FromMinutes(60));
var transport = new HttpSseTransport(
    logger,
    sessionManager,
    basePath: "/mcp",
    port: 5050
);

// Start transport
await transport.StartAsync();

// Register message handler
transport.MessageReceived += (sender, args) =>
{
    Console.WriteLine($"Received: {args.Message} from session {args.SessionId}");
};

// Stop transport
await transport.StopAsync();
```

---

## Security Considerations

### Localhost Binding

- **Default:** Binds to `127.0.0.1` (localhost only)
- **Rationale:** Prevents external network access
- **Configuration:** Change `UseUrls($"http://127.0.0.1:{_port}")` to bind to other interfaces (not recommended)

### Session Security

- **Crypto-secure tokens:** RandomNumberGenerator.Fill (CSPRNG)
- **URL-safe encoding:** No special characters in tokens
- **Expiration:** 60-minute timeout prevents token reuse
- **No credentials:** No username/password authentication (localhost trust model)

### DoS Mitigation

- **Session limits:** Background cleanup removes expired sessions
- **Connection limits:** ASP.NET Core Kestrel default limits apply
- **Rate limiting:** Not implemented in Phase 1 (add in Phase 2 if needed)

---

## Testing

### Unit Tests

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~Transports"
```

**Coverage:**
- SessionManager: 100% (crypto-secure token generation, expiration, cleanup)
- HttpSseTransport: 85% (HTTP endpoints, session validation, SSE streaming)

### Manual Testing with curl

1. **Create session:**
```bash
curl -X POST http://127.0.0.1:5050/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"ping"}' \
  -i
```

2. **Open SSE stream:**
```bash
curl -X GET http://127.0.0.1:5050/mcp \
  -H "Mcp-Session-Id: <session-id>" \
  -N
```

3. **Send message:**
```bash
curl -X POST http://127.0.0.1:5050/mcp \
  -H "Content-Type: application/json" \
  -H "Mcp-Session-Id: <session-id>" \
  -d '{"jsonrpc":"2.0","id":1,"method":"palace_search","params":{"query":"test"}}'
```

4. **Delete session:**
```bash
curl -X DELETE http://127.0.0.1:5050/mcp \
  -H "Mcp-Session-Id: <session-id>"
```

---

## Troubleshooting

### Issue: "401 Unauthorized" on GET /mcp

**Cause:** Invalid or expired session

**Solution:**
1. Create new session via POST /mcp
2. Use returned `Mcp-Session-Id` header
3. Ensure session hasn't expired (60-minute timeout)

### Issue: SSE stream not receiving events

**Cause:** No active SSE connection

**Solution:**
1. Open GET /mcp connection before sending POST messages
2. Keep connection alive (don't close after first event)
3. Check server logs for connection errors

### Issue: Port already in use

**Cause:** Another process is using port 5050

**Solution:**
1. Change port in HttpSseTransport constructor
2. Or stop conflicting process: `netstat -ano | findstr :5050`

---

## Next Steps

- **Phase 2:** CLI integration (`mempalacenet mcp --transport sse`)
- **Phase 3:** Copilot Skill marketplace integration
- **Phase 4:** Add CORS configuration for browser clients
- **Phase 5:** Add authentication/authorization (beyond localhost)

---

## References

- [MCP Specification - HTTP/SSE Transport](https://modelcontextprotocol.io/docs/concepts/transports#http-with-sse)
- [Server-Sent Events (SSE) Specification](https://html.spec.whatwg.org/multipage/server-sent-events.html)
- [ASP.NET Core Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [.NET RandomNumberGenerator](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.randomnumbergenerator)
