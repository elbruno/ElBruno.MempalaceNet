# MemPalace.NET Examples

Welcome to the MemPalace.NET examples directory! These standalone projects demonstrate key features and usage patterns.

## Available Examples

### 1. Simple Memory Agent
**Location:** `examples/SimpleMemoryAgent/`  
**Complexity:** Beginner  
**Time:** 5-10 minutes

A straightforward console app demonstrating the core MemPalace workflow:
- Initialize a palace with in-memory storage
- Add memories with rich metadata (wings, rooms, tags)
- Perform semantic search using vector similarity
- Query memories by ID and metadata

**Skills Demonstrated:**
- Palace initialization and configuration
- Collection management
- Embedding generation and storage
- Semantic search queries
- Metadata organization

**Perfect for:** First-time users, learning the API basics, understanding the palace structure

```bash
cd SimpleMemoryAgent
dotnet run
```

---

### 2. Semantic Knowledge Graph
**Location:** `examples/SemanticKnowledgeGraph/`  
**Complexity:** Intermediate  
**Time:** 10-15 minutes

Demonstrates building a temporal knowledge graph from documents:
- Extract entities and relationships from text
- Build triples (subject-predicate-object) with temporal validity
- Query the graph by patterns
- Track entity timelines and historical changes

**Skills Demonstrated:**
- Temporal triple creation and management
- Entity reference modeling
- Pattern-based graph queries
- Timeline queries (what was true when)
- Document processing and entity extraction

**Perfect for:** Building AI agent memory, tracking relationships over time, historical queries

```bash
cd SemanticKnowledgeGraph
dotnet run
```

---

## Quick Start

All examples are self-contained and can be run independently:

```bash
# Clone the repository
git clone https://github.com/elbruno/mempalacenet
cd mempalacenet/examples

# Run any example
cd SimpleMemoryAgent
dotnet run

# Or
cd SemanticKnowledgeGraph
dotnet run
```

## Prerequisites

- .NET 10.0 SDK or later
- No API keys required (uses in-memory backends and demo embedders)
- All dependencies are NuGet packages at version `0.1.0-preview.1`

## Example Comparison

| Feature | Simple Memory Agent | Semantic Knowledge Graph |
|---------|---------------------|-------------------------|
| **Focus** | Core memory operations | Temporal relationships |
| **Storage** | In-memory backend | SQLite knowledge graph |
| **Embeddings** | Demo embedder | N/A (graph-based) |
| **Complexity** | ~180 lines | ~250 lines |
| **Use Case** | Memory storage & search | Entity relationships & timelines |
| **Time to Run** | < 1 minute | < 1 minute |

## Learning Path

**Recommended order:**

1. **Start with SimpleMemoryAgent**
   - Understand palace structure (wings, rooms, drawers)
   - Learn the core API (backend, collections, embedders)
   - See how semantic search works

2. **Then SemanticKnowledgeGraph**
   - Learn temporal relationship modeling
   - Practice entity extraction
   - Understand graph queries and timelines

3. **Next: Explore the CLI**
   ```bash
   dotnet tool install -g mempalacenet --version 0.1.0-preview.1
   mempalacenet --help
   ```

4. **Finally: Build Your Own**
   - Combine memory storage + knowledge graph
   - Use real embedders (ONNX, OpenAI, Ollama)
   - Integrate with your application

## Common Use Cases

### Personal Knowledge Management
- Mine notes, articles, and documents
- Build a semantic search over your knowledge base
- Track topics and concepts over time

**Start with:** SimpleMemoryAgent → Add file mining

### AI Agent Memory
- Store agent observations and decisions
- Track entity relationships (users, projects, tasks)
- Query historical context for agent reasoning

**Start with:** SemanticKnowledgeGraph → Add semantic search

### Team Documentation
- Organize project knowledge by wings/rooms
- Extract entities from meeting notes and updates
- Build searchable team knowledge base

**Start with:** Both examples → Combine patterns

### Research & Learning
- Store research papers and summaries
- Build concept graphs from literature
- Track learning progress over time

**Start with:** SimpleMemoryAgent → Add knowledge graph

## Next Steps

After completing the examples:

### 1. Switch to Persistent Storage
```csharp
// Replace InMemoryBackend with SqliteBackend
var backend = new SqliteBackend("./my-palace-data");
```

### 2. Use Real Embedders
```csharp
// ONNX (local, no API keys)
services.AddMemPalaceAi(); // Uses ONNX by default

// Or OpenAI
services.AddMemPalaceAi(options => 
    options.Provider = AiProvider.OpenAI);
```

### 3. Add File Mining
```csharp
services.AddMemPalaceMining();
var miner = serviceProvider.GetRequiredService<FileSystemMiner>();
await miner.MineDirectoryAsync("./my-code", "work");
```

### 4. Enable Hybrid Search
```csharp
services.AddMemPalaceSearch();
var searchService = serviceProvider.GetRequiredService<HybridSearchService>();
var results = await searchService.SearchAsync("query", rerank: true);
```

### 5. Start an MCP Server
```bash
mempalacenet mcp --palace ~/my-palace
# Then configure Claude Desktop or VS Code
```

## Documentation

- **Main README:** `../README.md`
- **Architecture:** `../docs/architecture.md`
- **Concepts:** `../docs/PLAN.md`
- **CLI Reference:** `../docs/cli.md`
- **MCP Server:** `../docs/mcp.md`
- **API Docs:** Each package includes XML doc comments

## Community & Support

- **GitHub Issues:** [github.com/elbruno/mempalacenet/issues](https://github.com/elbruno/mempalacenet/issues)
- **Discussions:** [github.com/elbruno/mempalacenet/discussions](https://github.com/elbruno/mempalacenet/discussions)
- **License:** MIT

## Contributing

Found a bug in an example? Have an idea for a new example?

1. Fork the repository
2. Create a feature branch
3. Add your example in `examples/YourExample/`
4. Update this README
5. Submit a pull request

**Example ideas we'd love to see:**
- LLM-powered chat with memory
- Multi-agent collaboration with shared memory
- Real-time knowledge graph updates
- Integration with popular note-taking apps
- Document Q&A with semantic search

---

**Happy hacking! 🏛️**

Built with ❤️ by Bruno Capuano
