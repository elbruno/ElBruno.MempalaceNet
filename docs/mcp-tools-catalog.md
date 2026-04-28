# MCP Tool Catalog

Comprehensive reference for all MemPalace.NET MCP tools, including Phase 2 write operations.

---

## Overview

MemPalace.NET exposes **15 MCP tools** across three categories:
- **Read Operations (7 tools):** Query and retrieve data without modification
- **Write Operations (8 tools):** Create, update, and delete palace data
- **Health & Diagnostics (1 tool):** System health checks

---

## Read Operations

### 1. `palace_search`

**Description:** Search for memories matching a query using semantic search.

**Parameters:**
- `query` (string, required): Search query text
- `collection` (string, optional): Collection name (default: "memories")
- `topK` (int, optional): Number of results (default: 10, max: 100)
- `wing` (string, optional): Filter by wing
- `rerank` (bool, optional): Apply LLM reranking (default: false)

**Returns:**
```json
{
  "results": [
    {
      "id": "mem_abc123",
      "score": 0.95,
      "content": "Memory content here...",
      "metadata": {
        "wing": "conversations",
        "room": "planning",
        "timestamp": "2024-04-28T10:30:00Z"
      }
    }
  ]
}
```

**CLI Example:**
```bash
mempalacenet search "vector database design" --wing docs --rerank --top-k 5
```

**MCP JSON-RPC Example:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "palace_search",
    "arguments": {
      "query": "vector database design",
      "wing": "docs",
      "rerank": true,
      "topK": 5
    }
  }
}
```

**Error Codes:**
- `400`: Invalid query (empty or malformed)
- `404`: Collection or wing not found
- `500`: Search execution failed (embedder error)

---

### 2. `palace_recall`

**Description:** Recall memories from the palace (conversational alias for `palace_search`).

**Parameters:**
- `query` (string, required): Recall query
- `collection` (string, optional): Collection name (default: "memories")
- `topK` (int, optional): Number of memories to recall (default: 10)

**Returns:** Same format as `palace_search`.

**Use Case:** Framed for conversational retrieval (e.g., "What did I learn about X?").

---

### 3. `palace_get`

**Description:** Retrieve a specific memory by its unique ID.

**Parameters:**
- `id` (string, required): Memory ID (e.g., "mem_abc123")
- `collection` (string, optional): Collection name (default: "memories")

**Returns:**
```json
{
  "id": "mem_abc123",
  "content": "Full memory content...",
  "metadata": {
    "wing": "conversations",
    "room": "planning",
    "timestamp": "2024-04-28T10:30:00Z",
    "source": "chat-export.jsonl"
  }
}
```

**Error Codes:**
- `404`: Memory not found
- `500`: Database error

---

### 4. `palace_list_wings`

**Description:** List all wings (top-level categories) in the palace.

**Parameters:**
- `collection` (string, optional): Collection name (default: "memories")

**Returns:**
```json
{
  "wings": [
    {
      "name": "conversations",
      "memoryCount": 487,
      "lastUpdated": "2024-04-28T10:30:00Z"
    },
    {
      "name": "code",
      "memoryCount": 352,
      "lastUpdated": "2024-04-27T15:20:00Z"
    }
  ]
}
```

**CLI Example:**
```bash
mempalacenet wake-up --wing conversations --limit 20
```

---

### 5. `kg_query`

**Description:** Query the knowledge graph for entity relationships (triples).

**Parameters:**
- `subject` (string, optional): Subject entity or `?` for wildcard (e.g., "agent:roy")
- `predicate` (string, optional): Relationship type or `?` (e.g., "worked-on")
- `object` (string, optional): Object entity or `?` (e.g., "project:MemPalace.Mcp")
- `at` (string, optional): Temporal query timestamp (ISO8601)

**Returns:**
```json
{
  "triples": [
    {
      "subject": "agent:roy",
      "predicate": "worked-on",
      "object": "project:MemPalace.Mcp",
      "validFrom": "2024-04-24T00:00:00Z",
      "validTo": null
    }
  ]
}
```

**CLI Example:**
```bash
mempalacenet kg query agent:roy --type worked-on --as-of 2024-04-28
```

**MCP JSON-RPC Example:**
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "kg_query",
    "arguments": {
      "subject": "agent:roy",
      "predicate": "worked-on",
      "object": "?",
      "at": "2024-04-28T00:00:00Z"
    }
  }
}
```

---

### 6. `kg_timeline`

**Description:** Get a timeline of events (relationships) for an entity over time.

**Parameters:**
- `entity` (string, required): Entity reference (e.g., "agent:roy")
- `from` (string, optional): Start date (ISO8601)
- `to` (string, optional): End date (ISO8601)

**Returns:**
```json
{
  "timeline": [
    {
      "timestamp": "2024-04-24T00:00:00Z",
      "entity": "agent:roy",
      "predicate": "started",
      "other": "project:MemPalace.Mcp",
      "direction": "outgoing"
    }
  ]
}
```

**CLI Example:**
```bash
mempalacenet kg timeline agent:roy --from 2024-04-01 --to 2024-04-30
```

---

### 7. `palace_health`

**Description:** Check palace backend and embedder health.

**Parameters:** None

**Returns:**
```json
{
  "ok": true,
  "detail": "Palace operational",
  "backend": "Sqlite",
  "embedder": "Local ONNX",
  "collections": 1,
  "totalMemories": 1247
}
```

**CLI Example:**
```bash
mempalacenet health
```

**Error Codes:**
- `500`: Backend unavailable, embedder error, or configuration issue

---

## Write Operations

### 8. `palace_store`

**Description:** Store a new memory in the palace.

**Parameters:**
- `content` (string, required): Memory content
- `wing` (string, optional): Wing name (default: "default")
- `room` (string, optional): Room name
- `metadata` (object, optional): Additional metadata (key-value pairs)
- `collection` (string, optional): Collection name (default: "memories")

**Returns:**
```json
{
  "id": "mem_xyz789",
  "stored": true
}
```

**CLI Example:**
```bash
# Note: CLI doesn't have direct store command yet (Phase 2+)
# Use `mempalacenet mine` instead for batch storage
```

**MCP JSON-RPC Example:**
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "palace_store",
    "arguments": {
      "content": "Team meeting notes: Discussed CLI UX improvements",
      "wing": "conversations",
      "room": "meetings",
      "metadata": {
        "source": "manual-entry",
        "attendees": "team"
      }
    }
  }
}
```

**Error Codes:**
- `400`: Invalid content (empty or too large)
- `500`: Embedder error or database write failure

**Security:** Requires user confirmation in MCP clients (write operation).

---

### 9. `palace_update`

**Description:** Update an existing memory's content or metadata.

**Parameters:**
- `id` (string, required): Memory ID
- `content` (string, optional): New content (if updating content)
- `metadata` (object, optional): Metadata to merge or replace
- `collection` (string, optional): Collection name (default: "memories")

**Returns:**
```json
{
  "id": "mem_abc123",
  "updated": true
}
```

**Error Codes:**
- `404`: Memory not found
- `400`: Invalid update (no changes provided)
- `500`: Database error or embedder failure (if content changed)

**Security:** Requires user confirmation.

---

### 10. `palace_delete`

**Description:** Delete a memory by ID.

**Parameters:**
- `id` (string, required): Memory ID
- `collection` (string, optional): Collection name (default: "memories")

**Returns:**
```json
{
  "id": "mem_abc123",
  "deleted": true
}
```

**Error Codes:**
- `404`: Memory not found
- `500`: Database error

**Security:** Requires user confirmation.

---

### 11. `palace_batch_store`

**Description:** Store multiple memories in a single operation (bulk insert).

**Parameters:**
- `memories` (array, required): Array of memory objects (each with `content`, `wing`, `room`, `metadata`)
- `collection` (string, optional): Collection name (default: "memories")

**Returns:**
```json
{
  "stored": 10,
  "ids": ["mem_001", "mem_002", "..."]
}
```

**CLI Example:**
```bash
# Use mine command for bulk operations
mempalacenet mine ./docs --wing documentation --mode files
```

**Error Codes:**
- `400`: Invalid batch (empty or malformed)
- `500`: Partial failure (returns list of failed items)

**Security:** Requires user confirmation.

---

### 12. `kg_add_entity`

**Description:** Add a new entity to the knowledge graph.

**Parameters:**
- `entityId` (string, required): Unique entity ID (e.g., "person:alice")
- `entityType` (string, required): Entity type (e.g., "person", "project", "document")
- `properties` (object, optional): Entity properties (key-value pairs)

**Returns:**
```json
{
  "entityId": "person:alice",
  "created": true
}
```

**CLI Example:**
```bash
mempalacenet kg add-entity person:alice --type person --props '{"name":"Alice Smith","role":"engineer"}'
```

**MCP JSON-RPC Example:**
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "method": "tools/call",
  "params": {
    "name": "kg_add_entity",
    "arguments": {
      "entityId": "person:alice",
      "entityType": "person",
      "properties": {
        "name": "Alice Smith",
        "role": "engineer"
      }
    }
  }
}
```

**Error Codes:**
- `400`: Invalid entity ID or type
- `409`: Entity already exists
- `500`: Database error

**Security:** Requires user confirmation.

---

### 13. `kg_add_relationship`

**Description:** Add a relationship (triple) between two entities.

**Parameters:**
- `subject` (string, required): Subject entity ID
- `predicate` (string, required): Relationship type
- `object` (string, required): Object entity ID
- `validFrom` (string, optional): Start date (ISO8601)
- `validTo` (string, optional): End date (ISO8601, null for ongoing)

**Returns:**
```json
{
  "subject": "person:alice",
  "predicate": "works-on",
  "object": "project:mempalacenet",
  "created": true
}
```

**CLI Example:**
```bash
mempalacenet kg add-relationship person:alice project:mempalacenet works-on --valid-from 2024-01-01
```

**Error Codes:**
- `400`: Invalid relationship (missing entities or predicate)
- `404`: Subject or object entity not found
- `500`: Database error

**Security:** Requires user confirmation.

---

### 14. `palace_create_collection`

**Description:** Create a new collection (isolated palace namespace).

**Parameters:**
- `collection` (string, required): Collection name

**Returns:**
```json
{
  "collection": "research",
  "created": true
}
```

**Error Codes:**
- `409`: Collection already exists
- `400`: Invalid collection name
- `500`: Database error

**Security:** Requires user confirmation.

---

### 15. `palace_delete_collection`

**Description:** Delete a collection and all its memories.

**Parameters:**
- `collection` (string, required): Collection name
- `confirm` (bool, required): Must be `true` to prevent accidental deletion

**Returns:**
```json
{
  "collection": "old-data",
  "deleted": true,
  "memoriesDeleted": 523
}
```

**Error Codes:**
- `404`: Collection not found
- `400`: Confirmation not provided
- `500`: Database error

**Security:** Requires user confirmation AND explicit `confirm` parameter.

---

## Error Handling

All MCP tools return errors in the standard JSON-RPC format:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "error": {
    "code": 404,
    "message": "Memory not found",
    "data": {
      "memoryId": "mem_invalid",
      "remediation": [
        "Check the memory ID for typos",
        "List available memories with palace_search",
        "Verify the collection name"
      ]
    }
  }
}
```

**Common Error Codes:**
- `400`: Bad Request (invalid parameters)
- `404`: Not Found (resource doesn't exist)
- `409`: Conflict (resource already exists)
- `500`: Internal Server Error (backend, embedder, or unexpected failure)

---

## Security Considerations

### Write Operations

All write operations (store, update, delete, kg_add_*, etc.) **require user confirmation** in MCP clients. This is enforced by the MCP protocol and cannot be bypassed.

**Claude Desktop:** Prompts user before executing write operations.
**VS Code:** Shows confirmation dialog with operation details.

### Read Operations

Read operations (search, get, query) **do not require confirmation** and execute immediately.

### API Keys & Credentials

- Local ONNX embeddings: No API keys required (default)
- OpenAI/Azure embedders: API keys must be configured in palace settings
- MCP server never exposes API keys in responses or logs

---

## Performance Tips

### Search Optimization

1. **Use `wing` filter:** Narrows search scope for faster results
2. **Limit `topK`:** Default is 10; reduce for faster queries
3. **Disable reranking:** Only use `rerank: true` when precision is critical (slower)

### Batch Operations

1. **Use `palace_batch_store`:** 10x faster than individual `palace_store` calls
2. **Use CLI `mine` command:** Optimized for bulk file ingestion

### Knowledge Graph Queries

1. **Specify temporal bounds:** Use `at`, `from`, `to` to reduce result set
2. **Use specific predicates:** Wildcard queries (`?`) are slower

---

## Integration Examples

### Claude Desktop

```json
{
  "mcpServers": {
    "mempalace": {
      "command": "mempalacenet",
      "args": ["mcp"]
    }
  }
}
```

Add to: `%APPDATA%\Claude\claude_desktop_config.json` (Windows) or `~/Library/Application Support/Claude/claude_desktop_config.json` (macOS)

### VS Code

```json
{
  "mcp.servers": {
    "mempalace": {
      "command": "mempalacenet",
      "args": ["mcp"]
    }
  }
}
```

Add to: `.vscode/settings.json`

### GitHub Copilot CLI

```bash
# MCP tools are auto-discovered when mempalacenet is installed
gh copilot config set mcp.mempalace.enabled true
```

---

## CLI to MCP Mapping

| CLI Command | MCP Tool | Notes |
|-------------|----------|-------|
| `mempalacenet search "query"` | `palace_search` | Direct mapping |
| `mempalacenet wake-up --wing X` | `palace_list_wings` + `palace_search` | Combined operation |
| `mempalacenet mine /path --wing X` | `palace_batch_store` | Bulk store |
| `mempalacenet kg query X` | `kg_query` | Direct mapping |
| `mempalacenet kg timeline X` | `kg_timeline` | Direct mapping |
| `mempalacenet health` | `palace_health` | Direct mapping |

---

## Troubleshooting

### Tool Not Found

**Error:** `Tool 'palace_search' not found`

**Remediation:**
1. Verify MCP server is running: `mempalacenet mcp`
2. Check client configuration (Claude Desktop or VS Code)
3. Restart MCP client after configuration changes

### Write Operation Blocked

**Error:** `Write operation requires confirmation`

**Remediation:**
1. This is expected behavior (security feature)
2. Approve the operation in the MCP client UI
3. For automated workflows, use CLI instead of MCP tools

### Search Returns No Results

**Error:** `No memories found`

**Remediation:**
1. Verify palace contains memories: `mempalacenet health`
2. Check wing filter: remove or verify wing name
3. Mine content first: `mempalacenet mine /path --wing docs`

---

## Additional Resources

- **MCP Specification:** https://modelcontextprotocol.io/
- **C# SDK Docs:** https://csharp.sdk.modelcontextprotocol.io/
- **MemPalace MCP Source:** `src/MemPalace.Mcp/MemPalaceMcpTools.cs`
- **Troubleshooting Guide:** [troubleshooting.md](troubleshooting.md)
- **CLI Reference:** [cli.md](cli.md)
