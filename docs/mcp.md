# MCP Server

MemPalace.NET exposes a Model Context Protocol (MCP) server, allowing AI assistants like Claude Desktop, VS Code, and other MCP-compatible tools to interact with your palace.

## What is MCP?

The [Model Context Protocol](https://modelcontextprotocol.io/) is an open standard for connecting AI assistants to external data sources and tools. The MemPalace MCP server exposes your palace's search, retrieval, and knowledge graph capabilities as MCP tools.

## Starting the Server

The server runs over stdio (standard input/output) by default, which is what most MCP clients expect:

```bash
mempalacenet mcp
```

The server will continue running until stopped (Ctrl+C).

## MCP Tools Reference

The MemPalace MCP server exposes **15 tools** organized by category:

### Read Operations

#### `palace_search`
Search for memories in the palace matching a query.

**Parameters:**
- `query` (string, required): The search query text
- `collection` (string, optional): The collection/wing to search (default: "default")
- `topK` (int, optional): Number of results to return (default: 10)
- `wing` (string, optional): Filter results by wing
- `rerank` (bool, optional): Whether to apply reranking (default: false)

**Returns:** Array of search hits with `id`, `document`, `score`, and `metadata`.

**Example:**
```json
{
  "query": "vector database design",
  "collection": "default",
  "topK": 5,
  "rerank": true
}
```

#### `palace_recall`
Recall memories from the palace (alias for `palace_search`, framed for conversational recall).

**Parameters:**
- `query` (string, required): The recall query text
- `collection` (string, optional): The collection/wing to recall from (default: "default")
- `topK` (int, optional): Number of memories to recall (default: 10)

**Returns:** Same as `palace_search`.

#### `palace_get`
Get a specific memory by its unique ID.

**Parameters:**
- `id` (string, required): The unique ID of the memory
- `collection` (string, optional): The collection/wing (default: "default")
- `palace` (string, optional): The palace reference (default: "default")

**Returns:** Object with `id`, `document`, and `metadata`.

#### `palace_list_wings`
List all wings (collections) available in the palace.

**Parameters:**
- `palace` (string, optional): The palace reference (default: "default")

**Returns:** Array of wing names.

#### `palace_health`
Check the health and status of the MemPalace backend.

**Parameters:** None

**Returns:** Object with `ok` (boolean) and `detail` (string).

---

### Write Operations

#### `palace_store_memory`
Store a new memory in the palace. Embeds the content and stores it in the specified wing/collection.

**Parameters:**
- `content` (string, required): The memory content to store
- `collection` (string, optional): The collection/wing to store in (default: "default")
- `palace` (string, optional): The palace reference (default: "default")
- `metadata` (string, optional): Optional metadata as JSON object

**Returns:** Object with `memoryId` (string) and `storedAt` (ISO8601 timestamp).

**Example:**
```json
{
  "content": "Meeting notes: Q1 planning session",
  "collection": "meetings",
  "metadata": "{\"attendees\":[\"Alice\",\"Bob\"],\"date\":\"2024-01-15\"}"
}
```

#### `palace_update_memory`
Update an existing memory's content and/or metadata. Re-embeds if content changes.

**Parameters:**
- `id` (string, required): The unique ID of the memory to update
- `collection` (string, optional): The collection/wing containing the memory (default: "default")
- `palace` (string, optional): The palace reference (default: "default")
- `content` (string, optional): New content (leave empty to keep existing)
- `metadata` (string, optional): New metadata as JSON object (leave empty to keep existing)

**Returns:** Object with `memoryId` (string) and `updatedAt` (ISO8601 timestamp).

**Example:**
```json
{
  "id": "abc123",
  "content": "Updated meeting notes with action items",
  "collection": "meetings"
}
```

#### `palace_delete_memory`
Delete a memory from the palace by its unique ID.

**Parameters:**
- `id` (string, required): The unique ID of the memory to delete
- `collection` (string, optional): The collection/wing containing the memory (default: "default")
- `palace` (string, optional): The palace reference (default: "default")

**Returns:** Object with `deleted` (boolean) and `memoryId` (string).

**Example:**
```json
{
  "id": "abc123",
  "collection": "meetings"
}
```

---

### Bulk Operations

#### `palace_export_wing`
Export all memories from a wing/collection to JSON or CSV format.

**Parameters:**
- `collection` (string, optional): The collection/wing to export (default: "default")
- `palace` (string, optional): The palace reference (default: "default")
- `format` (string, optional): Output format: `"json"` or `"csv"` (default: "json")

**Returns:** Object with `wing` (string), `memoryCount` (int), `format` (string), and `content` (string).

**Example:**
```json
{
  "collection": "meetings",
  "format": "json"
}
```

#### `palace_import_memories`
Import memories in bulk from a JSON array. Each item must have a `content` field, optional `id` and `metadata`.

**Parameters:**
- `jsonContent` (string, required): JSON array of memories to import
- `collection` (string, optional): The collection/wing to import into (default: "default")
- `palace` (string, optional): The palace reference (default: "default")

**Returns:** Object with `importedCount` (int) and `errors` (string array).

**Example:**
```json
{
  "jsonContent": "[{\"content\":\"Memory 1\",\"metadata\":{\"source\":\"import\"}},{\"content\":\"Memory 2\"}]",
  "collection": "meetings"
}
```

---

### Control Operations

#### `palace_wake_up`
Wake up recent memories from a wing and generate a natural language summary using local LLM. Gracefully falls back to raw list if LLM unavailable.

**Parameters:**
- `collection` (string, optional): The collection/wing to wake up from (default: "default")
- `palace` (string, optional): The palace reference (default: "default")
- `days` (int, optional): Number of days to look back (default: 7)
- `limit` (int, optional): Maximum number of memories to retrieve (default: 20)

**Returns:** Object with `summary` (string), `memoriesProcessed` (int), and `usedLlm` (boolean).

**Example:**
```json
{
  "collection": "meetings",
  "days": 30,
  "limit": 50
}
```

#### `palace_get_stats`
Get statistics about the palace: memory count, wing distribution, embedder identity, backend type.

**Parameters:**
- `palace` (string, optional): The palace reference (default: "default")

**Returns:** Object with `palaceId` (string), `memoryCount` (long), `wingCount` (int), `wingStats` (dictionary), `embedder` (string), and `backend` (string).

**Example:**
```json
{
  "palace": "default"
}
```

---

### Knowledge Graph Tools

#### `kg_query`
Query the knowledge graph for entity relationships (triples).

**Parameters:**
- `subject` (string, optional): Subject entity (e.g., `agent:roy`) or `?` for any
- `predicate` (string, optional): Relationship type (e.g., `worked-on`) or `?` for any
- `object` (string, optional): Object entity (e.g., `project:MemPalace.Mcp`) or `?` for any
- `at` (string, optional): Query as of this timestamp (ISO8601)

**Returns:** Array of triples with `subject`, `predicate`, `object`, `validFrom`, and `validTo`.

**Example:**
```json
{
  "subject": "agent:roy",
  "predicate": "worked-on",
  "object": "?"
}
```

#### `kg_timeline`
Get a timeline of events (relationships) for an entity over time.

**Parameters:**
- `entity` (string, required): Entity reference (e.g., `agent:roy`, `project:MemPalace.Core`)
- `from` (string, optional): Start date (ISO8601)
- `to` (string, optional): End date (ISO8601)

**Returns:** Array of timeline events with `timestamp`, `entity`, `predicate`, `other`, and `direction`.

## Claude Desktop Configuration

To use the MemPalace MCP server with Claude Desktop, add this to your Claude configuration file:

**Windows:** `%APPDATA%\Claude\claude_desktop_config.json`
**macOS:** `~/Library/Application Support/Claude/claude_desktop_config.json`

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

After restarting Claude Desktop, the MemPalace tools will be available in Claude's tool palette.

## VS Code Configuration

For VS Code with the MCP extension:

1. Install the MCP extension from the marketplace
2. Add the MemPalace server to your workspace settings (`.vscode/settings.json`):

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

## Technical Details

### Package

The MCP server is implemented using the `ModelContextProtocol` NuGet package (v1.2.0).

### Transport

Currently, only **stdio** transport is supported. This is the standard transport for MCP servers and works with all major MCP clients (Claude Desktop, VS Code, Copilot CLI).

SSE/HTTP transport may be added in a future release via the `ModelContextProtocol.AspNetCore` package.

### Service Dependencies

The MCP server requires:
- `ISearchService` (from `MemPalace.Search`)
- `IBackend` (from `MemPalace.Core`)
- `IKnowledgeGraph` (from `MemPalace.KnowledgeGraph`)

These must be registered in the DI container before starting the MCP server.

### Logging

The MCP server logs to stderr by default (since MCP protocol messages flow over stdout). Use standard .NET logging configuration to control log levels.

## Limitations

- **Preview Package:** The `ModelContextProtocol` package is stable (v1.2.0), but the MCP specification is still evolving. Breaking changes in future versions of the protocol or SDK may require updates.
- **Transport:** Only stdio is currently supported. SSE/HTTP transport requires additional dependencies.
- **Concurrency:** The server processes requests sequentially. Parallel tool invocations from the client are not yet supported.

## Next Steps

- Explore the [MCP specification](https://modelcontextprotocol.io/specification/) for advanced usage
- Check the [C# SDK documentation](https://csharp.sdk.modelcontextprotocol.io/) for implementation details
- See `src/MemPalace.Mcp/MemPalaceMcpTools.cs` for the tool implementations
