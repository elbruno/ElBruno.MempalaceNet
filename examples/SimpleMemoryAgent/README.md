# Simple Memory Agent Example

A straightforward console application demonstrating the core MemPalace.NET workflow.

## What This Example Shows

This example walks through the fundamental operations of MemPalace.NET:

1. **Initialize a Palace** — Create an in-memory palace (no persistence)
2. **Add Memories** — Insert memories with metadata (wing, room, tags, dates)
3. **Semantic Search** — Find relevant memories using vector similarity
4. **Query by Metadata** — Retrieve specific memories by ID

## Key Concepts Demonstrated

- **Palace Structure** — Wings, rooms, and organized memory storage
- **Embeddings** — Vector representations for semantic search (using a demo embedder)
- **Collections** — Named groups of memories within a palace
- **Metadata** — Rich context attached to each memory (dates, tags, categories)
- **Verbatim Storage** — All memories stored exactly as provided

## Running the Example

```bash
# Navigate to the example directory
cd examples/SimpleMemoryAgent

# Run the example
dotnet run
```

## Expected Output

You should see:

```
🏛️  MemPalace.NET - Simple Memory Agent Example

This example demonstrates the core memory workflow:
  ✓ Initialize palace with in-memory storage
  ✓ Add memories to wings (work, personal)
  ✓ Perform semantic search
  ✓ Query by metadata

📦 Step 1: Initializing palace...
✓ Created collection 'work-memories'

💾 Step 2: Adding memories...
✓ Added 4 memories to the palace

🔍 Step 3: Searching memories...

Query: 'How did I implement security features?'
Found 2 relevant memories:
  [1] Implemented user authentication with JWT tokens...
      📅 Date: 2025-01-15, 📂 Room: project-alpha
  [2] Discussed API design patterns with team...
      📅 Date: 2025-01-17, 📂 Room: meetings
...
```

## Code Structure

- **Program.cs** (~180 lines)
  - Palace initialization
  - Memory insertion with rich metadata
  - Semantic search examples
  - Metadata-based queries
  - DemoEmbedder implementation for testing

## Next Steps

After understanding this example, try:

1. **Switch to SQLite Backend**
   ```csharp
   var backend = new SqliteBackend("./my-palace-data");
   ```

2. **Use Real Embedders**
   ```csharp
   // ONNX (local, no API keys)
   services.AddMemPalaceAi(); // Uses ONNX by default
   
   // Or OpenAI
   services.AddMemPalaceAi(options => 
       options.Provider = AiProvider.OpenAI);
   ```

3. **Automatic File Mining**
   ```csharp
   services.AddMemPalaceMining();
   var miner = serviceProvider.GetRequiredService<FileSystemMiner>();
   await miner.MineDirectoryAsync("./my-code", "work");
   ```

4. **Hybrid Search with Reranking**
   ```csharp
   services.AddMemPalaceSearch();
   var searchService = serviceProvider.GetRequiredService<HybridSearchService>();
   var results = await searchService.SearchAsync("query", rerank: true);
   ```

## What You'll Learn

- How to create and manage a MemPalace instance
- The basic API surface (backend, collections, embedders)
- How semantic search works with embeddings
- The importance of metadata for organizing memories
- The verbatim storage principle (exact content preserved)

## Dependencies

- MemPalace.Core
- MemPalace.Backends.Sqlite (includes InMemoryBackend)
- MemPalace.Ai
- MemPalace.Mining
- MemPalace.Search

All packages are available on NuGet at version `0.1.0-preview.1`.
