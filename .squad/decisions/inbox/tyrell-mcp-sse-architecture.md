# ADR: MCP SSE Transport Architecture

**Decision ID:** tyrell-mcp-sse-architecture  
**Author:** Tyrell (Core Engine Dev)  
**Date:** 2025-04-27  
**Status:** 🟡 Proposed (Awaiting Bruno's Approval)  
**Target:** v0.7.0 (deferred to v0.8.0 per Deckard recommendation)

---

## Context

MemPalace.NET currently implements an MCP (Model Context Protocol) server using **stdio transport** only (Phase 7, commit history). This works for desktop clients (Claude Desktop, VS Code) but **blocks web-based integrations** like:

1. **GitHub Copilot CLI** (browser-based skill invocation)
2. **Web-based AI assistants** (any client without subprocess spawning)
3. **Copilot Skill Marketplace** (requires HTTP-accessible MCP server)
4. **Multi-client scenarios** (stdio = 1:1 client-server; HTTP = 1:N)

The MCP specification defines **Streamable HTTP transport** as the standard solution for these scenarios. Our current implementation has placeholders (`--transport sse` CLI flag returns error, `docs/mcp.md` mentions future HTTP support).

**Mission v070-mcp-sse-transport** requires:
- Design HTTP SSE transport layer
- Maintain stdio transport (non-breaking)
- Enable real-time agent communication over HTTP
- Foundation for skill marketplace CLI integration

---

## Decision

Implement **MCP Streamable HTTP transport** using ASP.NET Core + Server-Sent Events (SSE) as a **parallel transport option** alongside stdio.

### Architecture Components

#### 1. Transport Abstraction Layer

**File:** `src/MemPalace.Mcp/Transport/IMcpTransport.cs`

```csharp
namespace MemPalace.Mcp.Transport;

/// <summary>
/// Abstraction for MCP transport mechanisms (stdio, HTTP/SSE).
/// </summary>
public interface IMcpTransport
{
    /// <summary>
    /// Start the transport and begin listening for messages.
    /// </summary>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>
    /// Stop the transport gracefully.
    /// </summary>
    Task StopAsync(CancellationToken ct = default);
}
```

**Rationale:** stdio and HTTP have fundamentally different lifecycles (subprocess vs web server). Abstract the transport to keep MCP tool logic unchanged.

#### 2. HTTP/SSE Transport Implementation

**Package:** `MemPalace.Mcp.AspNetCore` (new project)

**Dependencies:**
- ASP.NET Core Minimal API (`Microsoft.AspNetCore.App` framework reference)
- `ModelContextProtocol` v1.2.0 (existing)
- `MemPalace.Mcp` (existing tools)

**Endpoints:**

```
POST /mcp          — Send JSON-RPC message (request/response/notification)
GET  /mcp          — Open SSE stream for server→client messages
DELETE /mcp        — Terminate session (optional)
```

**File Structure:**
```
src/MemPalace.Mcp.AspNetCore/
├── MemPalace.Mcp.AspNetCore.csproj
├── HttpSseTransport.cs          — IMcpTransport implementation
├── McpEndpoints.cs               — Minimal API endpoint registration
├── SseStreamManager.cs           — SSE connection lifecycle
├── SessionStore.cs               — Session ID → state mapping
└── README.md                     — Transport-specific docs
```

#### 3. HTTP Endpoint Specification

**POST /mcp** (Client → Server)
- **Headers:**
  - `Content-Type: application/json`
  - `Accept: application/json, text/event-stream`
  - `Mcp-Session-Id: <uuid>` (after initialization)
  - `Mcp-Protocol-Version: 2025-06-18`
- **Body:** Single JSON-RPC message (request/response/notification)
- **Response:**
  - **For requests:** `200 OK` + `Content-Type: text/event-stream` (SSE stream with response)
  - **For notifications/responses:** `202 Accepted` (no body)
  - **Errors:** `400 Bad Request`, `404 Not Found` (session expired)

**GET /mcp** (SSE Stream)
- **Headers:**
  - `Accept: text/event-stream`
  - `Mcp-Session-Id: <uuid>` (optional on first request)
  - `Last-Event-Id: <event-id>` (resumption)
- **Response:**
  - `200 OK` + `Content-Type: text/event-stream`
  - Stream format:
    ```
    id: 1
    data: {"jsonrpc":"2.0","method":"tools/list_changed"}

    id: 2
    data: {"jsonrpc":"2.0","result":{"tools":[...]}}
    ```

**DELETE /mcp** (Session Termination)
- **Headers:** `Mcp-Session-Id: <uuid>`
- **Response:** `204 No Content` or `405 Method Not Allowed`

#### 4. Session Management

**Strategy:** Cryptographically secure session IDs (GUID v4) stored server-side.

**Flow:**
1. Client sends `InitializeRequest` via POST (no session ID)
2. Server responds with `InitializeResult` + `Mcp-Session-Id: <uuid>` header
3. Client includes session ID in all subsequent requests
4. Server validates session on every request (404 if expired/missing)
5. Session timeout: 30 minutes idle (configurable)

**File:** `src/MemPalace.Mcp.AspNetCore/SessionStore.cs`

```csharp
public class SessionStore
{
    private readonly ConcurrentDictionary<string, SessionState> _sessions = new();
    private readonly TimeSpan _timeout = TimeSpan.FromMinutes(30);

    public string CreateSession(InitializeResult initResult)
    {
        var sessionId = Guid.NewGuid().ToString();
        _sessions[sessionId] = new SessionState
        {
            InitializeResult = initResult,
            CreatedAt = DateTimeOffset.UtcNow,
            LastActivity = DateTimeOffset.UtcNow
        };
        return sessionId;
    }

    public bool TryGetSession(string sessionId, out SessionState? state)
    {
        if (_sessions.TryGetValue(sessionId, out state))
        {
            if (DateTimeOffset.UtcNow - state.LastActivity > _timeout)
            {
                _sessions.TryRemove(sessionId, out _);
                state = null;
                return false;
            }
            state.LastActivity = DateTimeOffset.UtcNow;
            return true;
        }
        return false;
    }

    public void RemoveSession(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
    }
}
```

#### 5. SSE Stream Management

**File:** `src/MemPalace.Mcp.AspNetCore/SseStreamManager.cs`

**Responsibilities:**
- Track active SSE connections per session
- Broadcast JSON-RPC messages to correct stream
- Handle stream resumption (`Last-Event-Id`)
- Implement event ID sequencing (per-stream cursor)
- Close streams gracefully on session expiry

**Concurrency:** Support multiple streams per session (MCP spec allows this).

**Resumability:** Use `id` field on SSE events for cursor-based replay.

#### 6. CLI Integration

**File:** `src/MemPalace.Cli/Commands/McpCommand.cs`

**Changes:**
- Remove "not yet supported" error for `--transport sse`
- Branch on transport type:
  ```csharp
  if (settings.Transport.ToLowerInvariant() == "stdio")
  {
      builder.Services.AddMemPalaceMcpWithStdio();
  }
  else if (settings.Transport.ToLowerInvariant() == "sse")
  {
      builder.Services.AddMemPalaceMcpWithHttpSse(options =>
      {
          options.Port = settings.Port;
          options.BindToLocalhost = true; // Security: no 0.0.0.0
      });
  }
  ```
- Add project reference: `MemPalace.Cli` → `MemPalace.Mcp.AspNetCore`

**CLI Commands:**
```bash
# stdio (unchanged)
mempalacenet mcp --transport stdio

# HTTP/SSE (new)
mempalacenet mcp --transport sse --port 5050
```

**Output:**
```
MemPalace MCP server started (HTTP/SSE)
Listening on: http://127.0.0.1:5050/mcp
Protocol version: 2025-06-18

Press Ctrl+C to stop...
```

---

## Implementation Plan

### Phase 1: Transport Abstraction (2 days)
**Owner:** Tyrell

- [ ] Create `IMcpTransport` interface
- [ ] Refactor existing stdio code to implement `StdioTransport : IMcpTransport`
- [ ] Update `ServiceCollectionExtensions` to use transport abstraction
- [ ] Test: stdio still works (existing MCP tests pass)

**Deliverable:** Non-breaking refactor; stdio unchanged.

---

### Phase 2: HTTP/SSE Core (3 days)
**Owner:** Tyrell

- [ ] Create `MemPalace.Mcp.AspNetCore` project
- [ ] Implement `SessionStore` (session CRUD + timeout)
- [ ] Implement `SseStreamManager` (connection tracking, broadcast)
- [ ] Implement `HttpSseTransport : IMcpTransport` (ASP.NET Core host)
- [ ] Implement `McpEndpoints` (POST, GET, DELETE handlers)
- [ ] Add security validations:
  - Origin header check (DNS rebinding mitigation)
  - Localhost-only binding (127.0.0.1, not 0.0.0.0)
- [ ] Test: manual POST/GET with `curl` or Postman

**Deliverable:** Standalone HTTP server with SSE streams.

---

### Phase 3: CLI Integration (1 day)
**Owner:** Rachael (depends on Tyrell's Phase 2)

- [ ] Update `McpCommand` to support `--transport sse`
- [ ] Add `--port` option (default 5050)
- [ ] Add project reference: `MemPalace.Cli` → `MemPalace.Mcp.AspNetCore`
- [ ] Test: `mempalacenet mcp --transport sse` starts HTTP server

**Deliverable:** Working CLI command for HTTP/SSE.

---

### Phase 4: Testing & Documentation (2 days)
**Owner:** Bryant (tests), Tyrell (docs)

- [ ] **Unit tests:**
  - SessionStore: create, get, timeout, remove
  - SseStreamManager: connection lifecycle, event sequencing, resumption
- [ ] **Integration tests:**
  - POST /mcp → SSE response (InitializeRequest)
  - GET /mcp → SSE stream (server notifications)
  - Session expiry (404 after timeout)
  - Multiple concurrent streams
- [ ] **Manual testing:**
  - Claude Desktop MCP Inspector (if SSE support exists)
  - Custom HTTP client (Python `requests` + SSE)
- [ ] **Documentation:**
  - Update `docs/mcp.md` (remove "future release" caveat, add SSE section)
  - Create `docs/mcp-sse-guide.md` (HTTP endpoint reference, session flow, client examples)
  - Add troubleshooting section (CORS, Origin header, localhost binding)

**Deliverable:** Test coverage ≥80%, comprehensive docs.

---

### Phase 5: Skill Marketplace Integration (Post-v0.7.0)
**Owner:** Rachael

- [ ] Update Copilot Skill manifest (`.github/copilot-skill.yaml`) with HTTP endpoint reference
- [ ] Create skill invocation pattern (docs/SKILL_PATTERNS.md)
- [ ] Test with GitHub Copilot CLI (if available)

**Deliverable:** Skill marketplace-ready MCP server.

---

## Dependencies

### Upstream (Blocks This Work)
None — all dependencies already in place.

### Downstream (This Work Blocks)
1. **v070-skill-marketplace-cli** (Issue #X) — Rachael's skill CLI depends on HTTP/SSE transport
2. **Copilot Skill Publication** (v0.7.0 / v1.0) — Marketplace submission requires HTTP-accessible MCP server

---

## Risks & Mitigations

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| **ASP.NET Core bloat** (adds 10+ MB to CLI) | Medium | Low | Acceptable for web scenarios; stdio remains default |
| **Session management bugs** (race conditions, leaks) | Medium | High | Thorough unit tests, stress testing with multiple clients |
| **Security vulnerabilities** (DNS rebinding, CORS bypass) | Low | Critical | Follow MCP spec security warnings, localhost-only default |
| **MCP spec changes** (2025-06-18 → future) | Low | Medium | Pin to stable spec version, add version negotiation tests |
| **Skill marketplace delays** | High | Low | Transport works independently; marketplace is separate workstream |

---

## Security Considerations

### 1. DNS Rebinding Attack Mitigation
**Threat:** Attacker uses DNS rebinding to make browser send requests to local MCP server.

**Mitigation:**
- Validate `Origin` header on all requests
- Reject requests without `Origin` or with non-whitelisted origins
- Default whitelist: `http://localhost`, `http://127.0.0.1`, `https://copilot.github.com`

**Code:**
```csharp
app.Use(async (context, next) =>
{
    var origin = context.Request.Headers.Origin.FirstOrDefault();
    if (!IsAllowedOrigin(origin))
    {
        context.Response.StatusCode = 403;
        await context.Response.WriteAsync("Forbidden: Invalid Origin header");
        return;
    }
    await next();
});
```

### 2. Localhost-Only Binding
**Threat:** Server binds to 0.0.0.0 (all interfaces), exposing MCP to network.

**Mitigation:**
- Default bind: `127.0.0.1` (localhost only)
- CLI option `--bind-address` for advanced users (warn about security risk)

**Code:**
```csharp
builder.WebHost.UseKestrel(options =>
{
    options.Listen(IPAddress.Loopback, settings.Port); // 127.0.0.1
});
```

### 3. Session Hijacking
**Threat:** Attacker guesses session ID, impersonates client.

**Mitigation:**
- Use cryptographically secure session IDs (GUID v4 = 122 bits entropy)
- HTTP-only cookies (not applicable, using headers)
- Short timeout (30 minutes idle)

### 4. DoS via Stream Exhaustion
**Threat:** Attacker opens many SSE streams, exhausts server resources.

**Mitigation:**
- Limit streams per session (default: 5)
- Connection timeout (idle streams closed after 5 minutes)
- Rate limiting (global: 100 req/min per IP)

---

## Performance Characteristics

### Latency
- **stdio:** ~1ms (IPC overhead)
- **HTTP/SSE:** ~5-20ms (HTTP overhead + localhost TCP)
- **Expected degradation:** 5-10ms for local clients, negligible for web clients

### Throughput
- **stdio:** Single client, sequential requests
- **HTTP/SSE:** Multiple clients, concurrent requests (ASP.NET Core async I/O)
- **Expected improvement:** 10x concurrent client capacity

### Resource Usage
- **Memory:** +50MB (ASP.NET Core host, session store, SSE buffers)
- **CPU:** +5-10% idle (Kestrel web server)
- **Disk:** +10MB (MemPalace.Mcp.AspNetCore.dll)

---

## Alternatives Considered

### Alternative 1: WebSocket Transport
**Pros:** Bidirectional, lower latency than SSE
**Cons:** MCP spec only defines stdio + HTTP/SSE; custom implementation required
**Decision:** Rejected — stick to MCP spec for interoperability

### Alternative 2: gRPC Transport
**Pros:** Efficient binary protocol, strong typing
**Cons:** Not in MCP spec, overkill for JSON-RPC messages
**Decision:** Rejected — unnecessary complexity

### Alternative 3: HTTP Long Polling
**Pros:** No SSE dependency, works in older browsers
**Cons:** Higher latency, more HTTP overhead than SSE
**Decision:** Rejected — SSE is MCP standard, modern browser support is fine

---

## Open Questions (Needs Bruno's Input)

1. **Scope:** Include in v0.7.0 or defer to v0.8.0?
   - Deckard recommends v0.8.0 (focus v0.7.0 on wake-up + Ollama)
   - Impact: Skill marketplace publication may wait until v0.8.0

2. **Default transport:** Should CLI default to stdio or SSE?
   - Recommendation: stdio (backward compatible, simpler for desktop users)
   - SSE explicit opt-in: `--transport sse`

3. **Authentication:** Should HTTP transport support API keys / OAuth?
   - Current: No auth (localhost-only, trusted client assumption)
   - Future: Add `--auth-token` option for cloud deployments?

4. **CORS policy:** Whitelist specific origins or allow all localhost?
   - Recommendation: Strict whitelist (localhost + copilot.github.com)
   - CLI option: `--allowed-origins` for advanced users

---

## Success Criteria

- [ ] HTTP/SSE transport implementation passes all MCP spec conformance checks
- [ ] stdio transport remains unchanged (zero regression)
- [ ] CLI supports both transports with clear documentation
- [ ] Test coverage ≥80% (unit + integration)
- [ ] Security validations pass (Origin header, localhost binding)
- [ ] Documentation complete (endpoint reference, client examples, troubleshooting)
- [ ] Manual testing with real MCP client (Claude Desktop or custom)

---

## References

1. [MCP Transport Specification](https://modelcontextprotocol.io/docs/concepts/transports)
2. [SSE Standard (WHATWG)](https://html.spec.whatwg.org/multipage/server-sent-events.html)
3. [ModelContextProtocol NuGet (v1.2.0)](https://www.nuget.org/packages/ModelContextProtocol)
4. [ASP.NET Core Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
5. `.squad/decisions.md` — Phase 7 (MCP stdio implementation)
6. `.squad/decisions/inbox/deckard-v070-roadmap-proposal.md` — v0.7.0 scope discussion

---

## Handoff Notes for Rachael (Skill CLI)

### What You'll Get
After Tyrell completes this work, you'll have:

1. **HTTP endpoint:** `http://127.0.0.1:5050/mcp`
2. **CLI command:** `mempalacenet mcp --transport sse --port 5050`
3. **Session flow:** POST /mcp (init) → GET /mcp (stream) with session ID
4. **Documentation:** `docs/mcp-sse-guide.md` with curl examples

### What You Need to Build (v070-skill-marketplace-cli)
1. **Skill invocation wrapper:**
   - Parse skill manifest (`.github/copilot-skill.yaml`)
   - Map skill capabilities to MCP tools
   - HTTP client for POST/GET (use `HttpClient` + SSE parser)
2. **Session management:**
   - Store session ID after initialization
   - Include `Mcp-Session-Id` header on all requests
   - Handle 404 (session expired) → re-initialize
3. **Error handling:**
   - Retry on connection failure
   - Graceful degradation (fallback to stdio if SSE unavailable)
4. **Testing:**
   - Integration test: start MCP server, invoke skill, verify response
   - Mock SSE stream for unit tests

### Dependencies
- **Blocks:** Cannot start skill CLI until HTTP/SSE transport is complete
- **Parallel work:** Update Copilot Skill manifest (add HTTP endpoint reference)
- **Integration point:** `docs/SKILL_PATTERNS.md` (new pattern: HTTP invocation)

### Timeline
- Tyrell's work: ~8 days (2+3+1+2)
- Your work: ~5 days (after Tyrell Phase 2 complete)
- Critical path: Tyrell Phase 2 → Rachael Phase 3 → joint testing

---

## Next Steps

1. **Bruno's Decision:** Approve scope for v0.7.0 or defer to v0.8.0
2. **Issue Creation:** File GitHub issue for this ADR (if approved)
3. **Implementation:** Tyrell begins Phase 1 (transport abstraction)
4. **Team Review:** Architecture review after Phase 1 (before HTTP implementation)

---

*— Tyrell, Core Engine Dev*
