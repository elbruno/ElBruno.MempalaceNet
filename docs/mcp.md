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

The MemPalace MCP server exposes the following tools:

### Palace Tools

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
