# Development Guide

This guide is for developers who want to understand the internals of MemPalace.NET, extend its functionality, or contribute advanced features.

For contribution guidelines, see [CONTRIBUTING.md](CONTRIBUTING.md).

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Project Structure](#project-structure)
- [Key Files & Dependencies](#key-files--dependencies)
- [How to Extend](#how-to-extend)
- [Testing Strategy](#testing-strategy)
- [Performance Considerations](#performance-considerations)
- [Debugging Tips](#debugging-tips)

---

## Architecture Overview

MemPalace.NET is a modular .NET solution with clear separation of concerns. Full details are in [docs/architecture.md](../docs/architecture.md).

### Core Principles

1. **Local-first by default** — No cloud calls required; ONNX embeddings run offline
2. **Abstraction over implementations** — `IBackend` and `IEmbedder` allow swapping storage and AI providers
3. **Microsoft.Extensions.AI integration** — First-class support for M.E.AI embedders and LLMs
4. **Testability** — Interfaces, dependency injection, and conformance tests ensure reliability

### Component Dependency Graph

```
MemPalace.Core (domain types, interfaces)
   ↓
   ├─→ MemPalace.Backends.Sqlite (SQLite storage)
   ├─→ MemPalace.Ai (embedder wrappers)
   ├─→ MemPalace.KnowledgeGraph (temporal entity graph)
   ├─→ MemPalace.Mining (content ingestion)
   └─→ MemPalace.Search (semantic + hybrid search)
       ↓
       ├─→ MemPalace.Mcp (Model Context Protocol server)
       ├─→ MemPalace.Agents (Microsoft Agent Framework integration)
       └─→ MemPalace.Cli (Spectre.Console CLI)
```

### Backend Contract

The `IBackend` interface defines how palaces and collections are managed:

```csharp
public interface IBackend
{
    Task<ICollection> GetCollectionAsync(PalaceRef palaceRef, string name, bool create, IEmbedder embedder);
    Task<List<string>> ListCollectionsAsync(PalaceRef palaceRef);
    Task DeleteCollectionAsync(PalaceRef palaceRef, string name);
    Task<HealthStatus> HealthAsync();
}
```

The `ICollection` interface defines storage and retrieval:

```csharp
public interface ICollection
{
    string Name { get; }
    int Dimensions { get; }
    string EmbedderIdentity { get; }

    Task AddAsync(IEnumerable<EmbeddedRecord> records);
    Task UpsertAsync(IEnumerable<EmbeddedRecord> records);
    Task<GetResult> GetAsync(IEnumerable<string>? ids, WhereClause? where, int? limit, int? offset, IncludeFlags include);
    Task<QueryResult> QueryAsync(IEnumerable<float[]> queryEmbeddings, int nResults, WhereClause? where, IncludeFlags include);
    Task<int> CountAsync();
    Task DeleteAsync(IEnumerable<string>? ids, WhereClause? where);
}
```

**Key Guarantees:**
- Collections store embedder identity to prevent dimension mismatches
- `AddAsync` throws on duplicate IDs; `UpsertAsync` does not
- `WhereClause` supports filtering by metadata (`Eq`, `In`, `And`, `Or`, etc.)
- Backends throw `UnsupportedFilterException` for unsupported queries

For full details, see [docs/architecture.md](../docs/architecture.md).

---

## Project Structure

MemPalace.NET is organized as a multi-project solution:

```
src/
├── MemPalace.Core/                # Domain types and interfaces
│   ├── Model/                     # PalaceRef, Wing, Room, Drawer, EmbeddedRecord
│   ├── Backends/                  # IBackend, ICollection, IEmbedder
│   │   └── InMemory/              # In-memory backend for testing
│   └── Errors/                    # BackendException hierarchy
│
├── MemPalace.Backends.Sqlite/     # Default SQLite backend
│   ├── SqliteBackend.cs           # Implements IBackend
│   ├── SqliteCollection.cs        # Implements ICollection with BLOB vectors
│   └── Migrations/                # Schema versioning
│
├── MemPalace.Ai/                  # Embedder wrappers
│   ├── OnnxEmbedder.cs            # Default: ElBruno.LocalEmbeddings (ONNX)
│   ├── OllamaEmbedder.cs          # Ollama integration
│   └── OpenAiEmbedder.cs          # OpenAI/Azure OpenAI integration
│
├── MemPalace.KnowledgeGraph/      # Temporal entity-relationship graph
│   ├── TemporalGraph.cs           # Triple store with validity windows
│   └── PatternQuery.cs            # SPARQL-like pattern matching
│
├── MemPalace.Mining/              # Content ingestion
│   ├── Miners/
│   │   ├── FilesystemMiner.cs     # Recursively mines directories
│   │   └── ConversationMiner.cs   # Parses conversation transcripts
│   └── MiningOptions.cs           # .gitignore respect, file filters
│
├── MemPalace.Search/              # Search strategies
│   ├── SemanticSearch.cs          # Vector similarity search
│   ├── HybridSearch.cs            # RRF fusion of semantic + keyword
│   ├── Reranker.cs                # LLM-based reranking
│   └── TemporalBoost.cs           # Recency scoring
│
├── MemPalace.Mcp/                 # Model Context Protocol server
│   ├── McpServer.cs               # MCP transport (stdio/SSE)
│   └── Tools/                     # 7 MCP tools (search, add, list, etc.)
│
├── MemPalace.Agents/              # Agent Framework integration
│   ├── AgentDiary.cs              # Per-agent memory management
│   └── AgentDiscovery.cs          # Agent registration and lookup
│
├── MemPalace.Cli/                 # CLI tool (Spectre.Console)
│   ├── Commands/                  # init, mine, search, agents, mcp
│   └── Program.cs                 # Entry point
│
├── MemPalace.Benchmarks/          # Performance and accuracy benchmarks
│   ├── LongMemEval/               # R@5 accuracy benchmarks
│   └── BenchmarkRunner.cs         # BenchmarkDotNet harness
│
└── MemPalace.Tests/               # xUnit test suite
    ├── Backends/                  # Backend conformance tests
    ├── Ai/                        # Embedder tests
    ├── Search/                    # Search strategy tests
    └── ...                        # Component-specific test folders
```

### Key Files

| File | Purpose |
|------|---------|
| `src/MemPalace.slnx` | Solution file (build with `dotnet build src/`) |
| `Directory.Build.props` | Shared MSBuild properties (version, nullable context) |
| `docs/architecture.md` | Architecture deep dive |
| `docs/PLAN.md` | Concepts (wings, rooms, drawers, verbatim storage) |
| `CHANGELOG.md` | Release notes and breaking changes |

---

## Key Files & Dependencies

### Dependencies (NuGet)

MemPalace.NET minimizes external dependencies:

| Package | Used In | Purpose |
|---------|---------|---------|
| `Microsoft.Extensions.AI` | Ai, Agents, Search | Embedder/LLM abstraction |
| `ElBruno.LocalEmbeddings` | Ai | Default ONNX embedder (offline) |
| `Microsoft.Data.Sqlite` | Backends.Sqlite | SQLite database access |
| `Spectre.Console` | Cli | Rich terminal UI |
| `xUnit` | Tests | Unit testing framework |
| `BenchmarkDotNet` | Benchmarks | Performance benchmarking |

### Adding Dependencies

Before adding a new NuGet package:
1. **Evaluate necessity** — Can you solve it without a dependency?
2. **Check license** — Must be compatible with MIT
3. **Open an issue** — Discuss with maintainers first
4. **Document rationale** — Explain why it's needed in the PR

---

## How to Extend

### Adding a New Backend

1. **Implement `IBackend` and `ICollection`**:

   ```csharp
   public class MyCustomBackend : IBackend
   {
       public async Task<ICollection> GetCollectionAsync(PalaceRef palaceRef, string name, bool create, IEmbedder embedder)
       {
           // Your implementation
       }
       // ... other methods
   }
   ```

2. **Subclass `BackendConformanceTests`**:

   ```csharp
   public class MyCustomBackendTests : BackendConformanceTests
   {
       protected override IBackend CreateBackend() => new MyCustomBackend();
   }
   ```

3. **Run conformance tests** to ensure compatibility:

   ```bash
   dotnet test src/MemPalace.Tests/ --filter MyCustomBackendTests
   ```

See [docs/backends.md](../docs/backends.md) for full details.

### Adding a New Embedder

1. **Implement `IEmbedder`**:

   ```csharp
   public class MyEmbedder : IEmbedder
   {
       public string ModelIdentity => "my-embedder-v1";
       public int Dimensions => 768;

       public async Task<ReadOnlyMemory<float>[]> EmbedAsync(string[] texts)
       {
           // Your implementation
       }
   }
   ```

2. **Register in `MemPalace.Ai`** (if contributing back):

   ```csharp
   public static IEmbedder CreateMyEmbedder(string apiKey)
   {
       return new MyEmbedder(apiKey);
   }
   ```

3. **Test with real collections**:

   ```csharp
   var embedder = new MyEmbedder();
   var collection = await backend.GetCollectionAsync(palaceRef, "test", true, embedder);
   ```

See [docs/ai.md](../docs/ai.md) for integration patterns.

### Adding a New Miner

1. **Implement content ingestion logic**:

   ```csharp
   public class MyCustomMiner
   {
       public async Task<List<EmbeddedRecord>> MineAsync(string sourcePath, IEmbedder embedder)
       {
           var records = new List<EmbeddedRecord>();
           // Parse your content format
           // Embed text chunks
           // Return records
           return records;
       }
   }
   ```

2. **Add CLI command** in `MemPalace.Cli/Commands/MineCommand.cs`:

   ```csharp
   if (mode == "mycustom")
   {
       var miner = new MyCustomMiner();
       var records = await miner.MineAsync(sourcePath, embedder);
       await collection.AddAsync(records);
   }
   ```

See [docs/mining.md](../docs/mining.md) for examples.

### Adding a New Search Strategy

1. **Implement search logic**:

   ```csharp
   public class MySearchStrategy
   {
       public async Task<List<SearchResult>> SearchAsync(ICollection collection, string query, IEmbedder embedder)
       {
           // Embed query
           // Query collection
           // Apply custom ranking
           return results;
       }
   }
   ```

2. **Add CLI flag** in `MemPalace.Cli/Commands/SearchCommand.cs`:

   ```csharp
   var strategy = options.Strategy == "mystrategy"
       ? new MySearchStrategy()
       : new SemanticSearch();
   ```

See [docs/search.md](../docs/search.md) for hybrid search and reranking patterns.

---

## Testing Strategy

### Test Pyramid

MemPalace.NET follows a standard test pyramid:

1. **Unit Tests** (~60%)
   - Test isolated logic (embedder wrappers, search scoring, metadata parsing)
   - Fast, deterministic, no I/O

2. **Integration Tests** (~30%)
   - Test backend operations (add, query, delete)
   - Use in-memory backend or SQLite with temp databases

3. **End-to-End Tests** (~10%)
   - Test CLI commands with real files
   - Verify MCP server communication

### Conformance Tests

The `BackendConformanceTests` abstract class ensures all backends behave identically:

```csharp
public abstract class BackendConformanceTests
{
    protected abstract IBackend CreateBackend();

    [Fact]
    public async Task AddAsync_ValidRecords_StoresSuccessfully() { }

    [Fact]
    public async Task QueryAsync_EmptyCollection_ReturnsEmpty() { }

    // ... 30+ conformance tests
}
```

To test a new backend, simply subclass:

```csharp
public class MyBackendConformanceTests : BackendConformanceTests
{
    protected override IBackend CreateBackend() => new MyBackend();
}
```

### Fake Embedder

For deterministic tests, use the `FakeEmbedder`:

```csharp
var embedder = new FakeEmbedder(dimensions: 384);
var embedding = await embedder.EmbedAsync(new[] { "test text" });
// Returns deterministic hash-based embedding
```

This avoids model downloads and ensures reproducible test results.

### Running Tests

```bash
# All tests
dotnet test src/

# Specific project
dotnet test src/MemPalace.Tests/

# Filter by category
dotnet test src/ --filter "FullyQualifiedName~Search"

# With coverage
dotnet test src/ --collect:"XPlat Code Coverage"
```

---

## Performance Considerations

### Vector Search

- **SQLite backend** uses brute-force cosine similarity (acceptable for <100K vectors)
- **Upgrade path**: Migrate to `sqlite-vec` or Qdrant for millions of vectors
- **Current performance**: ~50ms for 10K vectors on modern CPUs

### Embedding

- **ONNX (default)** runs offline but is CPU-bound (~100ms per text on laptop)
- **Ollama** uses local GPU if available (faster, still local)
- **OpenAI** fastest but requires network and API key

**Recommendation**: For bulk mining, batch texts and parallelize embedding:

```csharp
var batches = texts.Chunk(100);
var tasks = batches.Select(batch => embedder.EmbedAsync(batch));
var embeddings = await Task.WhenAll(tasks);
```

### Memory Usage

- **Embeddings in memory**: 384 dimensions × 4 bytes × N records = ~1.5KB per record
- **Large collections**: Use `ICollection.GetAsync` with pagination (`limit`, `offset`)
- **Streaming**: Process files in chunks to avoid loading entire collections

### Benchmarking

Run BenchmarkDotNet to profile performance:

```bash
dotnet run --project src/MemPalace.Benchmarks/ -c Release
```

See [docs/benchmarks.md](../docs/benchmarks.md) for details.

---

## Debugging Tips

### Logging

Enable detailed logging with `Microsoft.Extensions.Logging`:

```csharp
var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

var backend = new SqliteBackend(loggerFactory);
```

### Inspecting SQLite Databases

Open palace databases with SQLite CLI:

```bash
sqlite3 ~/my-palace/palace.db

.tables
.schema collections
SELECT * FROM collections LIMIT 10;
```

### Debugging Tests

Run tests with verbose output:

```bash
dotnet test src/ --logger "console;verbosity=detailed"
```

Set breakpoints in your IDE (VS, VS Code, Rider) and debug tests interactively.

### Common Issues

**Embedder identity mismatch:**
- Delete the collection: `mempalacenet delete-collection <name>`
- Recreate with the new embedder

**Dimension mismatch:**
- Ensure embedder returns correct dimensions (e.g., 384 for ONNX)
- Check `collection.Dimensions` property

**Slow queries:**
- Profile with BenchmarkDotNet
- Consider pagination for large result sets
- Upgrade to vector-optimized backend for >100K records

---

## Additional Resources

- **[Architecture](../docs/architecture.md)** — Deep dive into contracts and error model
- **[Concepts](../docs/PLAN.md)** — Wings, rooms, drawers, verbatim storage
- **[Backends](../docs/backends.md)** — Writing custom backends
- **[AI Integration](../docs/ai.md)** — Embedder selection, M.E.AI seams
- **[Mining](../docs/mining.md)** — Ingestion pipeline, custom miners
- **[Search](../docs/search.md)** — Semantic, hybrid, reranking strategies
- **[MCP Server](../docs/mcp.md)** — Tool reference, client setup
- **[Agents](../docs/agents.md)** — Agent Framework integration
- **[CLI](../docs/cli.md)** — Command reference

---

## Questions?

- **General questions** — [GitHub Discussions](https://github.com/elbruno/mempalacenet/discussions)
- **Bug reports or feature requests** — [GitHub Issues](https://github.com/elbruno/mempalacenet/issues)
- **Contributing** — See [CONTRIBUTING.md](CONTRIBUTING.md)

Happy hacking! 🚀
