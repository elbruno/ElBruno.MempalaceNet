# MemPalace.NET — GitHub Copilot Skill

**Status:** Preview  
**Version:** 0.5.0-preview.1  
**Category:** Knowledge Management  
**Author:** Bruno Capuano (@elbruno)

---

## What is MemPalace.NET?

MemPalace.NET is a **local-first AI memory library** that stores everything verbatim, searches semantically, and organizes knowledge through a hierarchical *wings/rooms/drawers* structure. It's a .NET port of the [MemPalace](https://github.com/MemPalace/mempalace) Python library, designed to integrate seamlessly with **Microsoft.Extensions.AI** and the **Microsoft Agent Framework**. With ONNX embeddings by default, it runs entirely locally without requiring API keys or cloud calls.

The library provides **semantic search** (vector similarity), **hybrid search** (combining keyword and semantic), **temporal knowledge graphs** (entity relationships with validity windows), and **MCP server** integration for exposing memory operations as tools.

---

## Why Use MemPalace.NET as a Copilot Skill?

### 1. **Teaching RAG Architecture Patterns**
MemPalace.NET demonstrates how to build Retrieval-Augmented Generation (RAG) systems locally. It shows developers how to:
- Embed and store documents/conversations
- Retrieve relevant context for LLM prompts
- Rerank results for precision
- Implement hybrid search (semantic + keyword)

### 2. **Local-First Privacy**
By using ONNX embeddings ([ElBruno.LocalEmbeddings](https://github.com/elbruno/LocalEmbeddings)), MemPalace.NET runs without external API calls. This is critical for:
- Privacy-sensitive applications (medical, legal, enterprise)
- Air-gapped environments
- Cost control (no per-token API charges)
- Offline-first development

### 3. **Agent Memory & Multi-Turn Context**
Each agent gets its own memory diary (a dedicated wing in the palace). This enables:
- Multi-turn conversation context persistence
- Agent state tracking across sessions
- Semantic retrieval of past interactions
- Integration with Microsoft Agent Framework

### 4. **Knowledge Graph for Structured Knowledge**
The temporal knowledge graph tracks entity relationships with validity windows:
- "Alice worked at Acme from 2020 to 2023"
- "Project X uses library Y since version 2.0"
- Temporal queries: "Who was the CEO in 2019?"

### 5. **MCP Server for Tool Exposure**
The built-in MCP server exposes palace operations as Model Context Protocol tools, making them accessible to:
- Claude Desktop
- VS Code extensions
- Custom agents
- Other MCP-compatible clients

---

## How to Integrate MemPalace.NET with GitHub Copilot

### Option 1: NuGet Library Integration
For programmatic use in .NET applications:

```bash
# Install the NuGet package
dotnet add package mempalacenet --version 0.5.0-preview.1
```

Then use Copilot to generate code like:

```csharp
using MemPalace;
using Microsoft.Extensions.AI;

// Initialize a palace with local embeddings
var palace = await Palace.Create("~/my-palace");

// Store a memory
await palace.Store(
    content: "Alice joined the engineering team in Q1 2024",
    metadata: new Dictionary<string, object> {
        { "source", "slack" },
        { "author", "alice@example.com" }
    },
    wing: "team-updates"
);

// Semantic search
var results = await palace.Search(
    query: "Who joined the team recently?",
    wing: "team-updates",
    limit: 5
);

foreach (var result in results) {
    Console.WriteLine($"Score: {result.Score:F3} | {result.Memory.Content}");
}
```

### Option 2: CLI Tool Integration
For command-line workflows:

```bash
# Install the CLI tool
dotnet tool install -g mempalacenet --version 0.5.0-preview.1

# Initialize a palace
mempalacenet init ~/my-palace

# Mine files
mempalacenet mine ~/my-code --wing work --mode files

# Semantic search
mempalacenet search "how do I handle auth errors?" --hybrid --rerank
```

### Option 3: MCP Server Integration
For Claude Desktop, VS Code, and other MCP clients:

```bash
# Start the MCP server
mempalacenet mcp-server --palace-path ~/my-palace
```

Then configure your MCP client to connect to the server. See [docs/mcp.md](./mcp.md) for full configuration.

---

## Example Use Cases

### 1. **RAG Context Injection**
Use MemPalace.NET to retrieve relevant context for LLM prompts:

```csharp
// Store documentation
await palace.Mine("~/docs", wing: "documentation", mode: MineMode.Files);

// Retrieve context for a coding question
var context = await palace.Search(
    query: "How do I implement OAuth2 authentication?",
    wing: "documentation",
    limit: 3
);

// Inject into LLM prompt
var prompt = $@"
Context from documentation:
{string.Join("\n\n", context.Select(r => r.Memory.Content))}

Question: How do I implement OAuth2 authentication in this codebase?
";
```

### 2. **Agent Memory Diaries**
Track agent state across sessions:

```csharp
// Each agent gets its own wing
var agentId = "customer-support-bot-42";
var wing = $"agents/{agentId}";

// Store interaction
await palace.Store(
    content: "User asked about refund policy. Provided link to FAQ.",
    metadata: new Dictionary<string, object> {
        { "userId", "user-12345" },
        { "timestamp", DateTime.UtcNow }
    },
    wing: wing
);

// Recall previous interactions
var history = await palace.Search(
    query: "refund policy discussions",
    wing: wing,
    limit: 10
);
```

### 3. **Knowledge Graph Queries**
Track temporal entity relationships:

```csharp
// Add a relationship
await palace.KnowledgeGraph.AddEntity(
    entityId: "alice",
    entityType: "person",
    properties: new { role = "engineer", team = "platform" }
);

await palace.KnowledgeGraph.AddRelationship(
    fromId: "alice",
    toId: "project-x",
    relationshipType: "works_on",
    validFrom: new DateTime(2024, 1, 1),
    validTo: null // ongoing
);

// Query relationships
var relationships = await palace.KnowledgeGraph.Query(
    entityId: "alice",
    relationshipType: "works_on",
    asOf: DateTime.UtcNow
);
```

### 4. **Hybrid Search with Reranking**
Combine semantic and keyword search for precision:

```bash
mempalacenet search "React hooks best practices" \
  --hybrid \
  --rerank \
  --wing frontend-docs \
  --limit 5
```

---

## Learning Resources

- **Main README:** [README.md](../README.md)
- **Implementation Plan:** [docs/PLAN.md](./PLAN.md)
- **Pattern Library:** [docs/SKILL_PATTERNS.md](./SKILL_PATTERNS.md)
- **Integration Checklist:** [docs/SKILL_INTEGRATION.md](./SKILL_INTEGRATION.md)
- **Examples:** [examples/README.md](../examples/README.md)
- **MCP Server:** [docs/mcp.md](./mcp.md)
- **CLI Reference:** [docs/cli.md](./cli.md)
- **Knowledge Graph:** [docs/kg.md](./kg.md)

---

## Support & Community

- **Issues:** [GitHub Issues](https://github.com/elbruno/mempalacenet/issues)
- **Discussions:** [GitHub Discussions](https://github.com/elbruno/mempalacenet/discussions)
- **Twitter:** [@elbruno](https://twitter.com/elbruno)
- **LinkedIn:** [Bruno Capuano](https://www.linkedin.com/in/elbruno/)

---

## License

MIT License — see [LICENSE](../LICENSE) for details.
