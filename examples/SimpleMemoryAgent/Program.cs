using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using MemPalace.Backends.Sqlite;
using MemPalace.Core.Backends.InMemory;

namespace SimpleMemoryAgent;

/// <summary>
/// Simple console app demonstrating the basic MemPalace.NET workflow:
/// 1. Initialize a palace with in-memory backend
/// 2. Add memories to different wings/rooms
/// 3. Search across memories using semantic similarity
/// 4. Query specific memories by metadata
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🏛️  MemPalace.NET - Simple Memory Agent Example\n");
        Console.WriteLine("This example demonstrates the core memory workflow:");
        Console.WriteLine("  ✓ Initialize palace with in-memory storage");
        Console.WriteLine("  ✓ Add memories to wings (work, personal)");
        Console.WriteLine("  ✓ Perform semantic search");
        Console.WriteLine("  ✓ Query by metadata\n");

        // Step 1: Initialize the palace with in-memory backend
        Console.WriteLine("📦 Step 1: Initializing palace...");
        await using var backend = new InMemoryBackend();
        
        var palace = new PalaceRef("my-first-palace");
        
        // Create a simple embedder (identity embedder for demo purposes)
        var embedder = new DemoEmbedder();
        
        // Get or create a collection in the "work" wing
        var workCollection = await backend.GetCollectionAsync(
            palace,
            "work-memories",
            create: true,
            embedder: embedder);
        
        Console.WriteLine($"✓ Created collection 'work-memories'\n");

        // Step 2: Add memories to the palace
        Console.WriteLine("💾 Step 2: Adding memories...");
        
        var memories = new[]
        {
            new EmbeddedRecord(
                Id: "mem-1",
                Document: "Implemented user authentication with JWT tokens. Added refresh token support and secure cookie storage.",
                Metadata: new Dictionary<string, object?>
                {
                    ["wing"] = "work",
                    ["room"] = "project-alpha",
                    ["date"] = "2025-01-15",
                    ["tags"] = new[] { "auth", "security", "jwt" }
                },
                Embedding: await embedder.EmbedAsync("user authentication JWT tokens", default)),
            
            new EmbeddedRecord(
                Id: "mem-2",
                Document: "Resolved database connection pooling issue. Increased max pool size to 100 and added connection timeout monitoring.",
                Metadata: new Dictionary<string, object?>
                {
                    ["wing"] = "work",
                    ["room"] = "project-alpha",
                    ["date"] = "2025-01-16",
                    ["tags"] = new[] { "database", "performance", "troubleshooting" }
                },
                Embedding: await embedder.EmbedAsync("database connection pooling", default)),
            
            new EmbeddedRecord(
                Id: "mem-3",
                Document: "Discussed API design patterns with team. Agreed on RESTful conventions and versioning strategy (v1, v2 prefixes).",
                Metadata: new Dictionary<string, object?>
                {
                    ["wing"] = "work",
                    ["room"] = "meetings",
                    ["date"] = "2025-01-17",
                    ["tags"] = new[] { "api", "design", "collaboration" }
                },
                Embedding: await embedder.EmbedAsync("API design patterns REST", default)),
            
            new EmbeddedRecord(
                Id: "mem-4",
                Document: "Learned about the CQRS pattern from a YouTube tutorial. Considering applying it to the reporting module.",
                Metadata: new Dictionary<string, object?>
                {
                    ["wing"] = "personal",
                    ["room"] = "learning",
                    ["date"] = "2025-01-18",
                    ["tags"] = new[] { "architecture", "cqrs", "learning" }
                },
                Embedding: await embedder.EmbedAsync("CQRS pattern architecture", default))
        };

        await workCollection.UpsertAsync(memories);
        Console.WriteLine($"✓ Added {memories.Length} memories to the palace\n");

        // Step 3: Demonstrate semantic search
        Console.WriteLine("🔍 Step 3: Searching memories...\n");
        
        // Search for authentication-related memories
        Console.WriteLine("Query: 'How did I implement security features?'");
        var authQuery = await embedder.EmbedAsync("security features authentication", default);
        var authResults = await workCollection.QueryAsync(
            authQuery,
            topK: 2,
            include: IncludeFields.All);
        
        Console.WriteLine($"Found {authResults.Ids.Count} relevant memories:");
        for (int i = 0; i < authResults.Ids.Count; i++)
        {
            Console.WriteLine($"  [{i + 1}] {authResults.Documents?[i]}");
            if (authResults.Metadata?[i] != null)
            {
                var meta = authResults.Metadata[i];
                Console.WriteLine($"      📅 Date: {meta["date"]}, 📂 Room: {meta["room"]}");
            }
        }
        Console.WriteLine();

        // Search for database-related issues
        Console.WriteLine("Query: 'What database problems have I encountered?'");
        var dbQuery = await embedder.EmbedAsync("database problems issues", default);
        var dbResults = await workCollection.QueryAsync(
            dbQuery,
            topK: 2,
            include: IncludeFields.All);
        
        Console.WriteLine($"Found {dbResults.Ids.Count} relevant memories:");
        for (int i = 0; i < dbResults.Ids.Count; i++)
        {
            Console.WriteLine($"  [{i + 1}] {dbResults.Documents?[i]}");
            if (dbResults.Metadata?[i] != null)
            {
                var meta = dbResults.Metadata[i];
                Console.WriteLine($"      📅 Date: {meta["date"]}, 📂 Room: {meta["room"]}");
            }
        }
        Console.WriteLine();

        // Step 4: Query by specific criteria
        Console.WriteLine("📋 Step 4: Querying by metadata...\n");
        
        // Get specific memory by ID
        Console.WriteLine("Getting memory 'mem-3' by ID:");
        var specificMemory = await workCollection.GetAsync(
            new[] { "mem-3" },
            include: IncludeFields.All);
        
        if (specificMemory.Documents?.Count > 0)
        {
            Console.WriteLine($"  {specificMemory.Documents[0]}");
        }
        Console.WriteLine();

        // List all collections
        Console.WriteLine("📚 Collections in palace:");
        var collections = await backend.ListCollectionsAsync(palace);
        foreach (var coll in collections)
        {
            Console.WriteLine($"  • {coll}");
        }
        Console.WriteLine();

        Console.WriteLine("✅ Example completed successfully!");
        Console.WriteLine("\n💡 Next steps:");
        Console.WriteLine("  • Replace InMemoryBackend with SqliteBackend for persistence");
        Console.WriteLine("  • Use real embedders (ONNX, OpenAI, Ollama) via MemPalace.Ai");
        Console.WriteLine("  • Explore the Mining module to ingest files automatically");
        Console.WriteLine("  • Try hybrid search with reranking for better results");
    }
}

/// <summary>
/// Simple demo embedder that creates deterministic embeddings based on text length and content.
/// In production, use real embedders from MemPalace.Ai (ONNX, OpenAI, Ollama).
/// </summary>
internal class DemoEmbedder : IEmbedder
{
    public int Dimensions => 384;
    public string ModelIdentity => "demo-embedder-v1";

    public Task<ReadOnlyMemory<float>> EmbedAsync(string text, CancellationToken ct = default)
    {
        // Create a simple deterministic embedding based on text content
        var embedding = new float[Dimensions];
        
        // Use text hash and length to create a pseudo-embedding
        var hash = text.GetHashCode();
        var random = new Random(hash);
        
        for (int i = 0; i < Dimensions; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2.0 - 1.0);
        }
        
        // Normalize the vector
        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
        for (int i = 0; i < Dimensions; i++)
        {
            embedding[i] /= (float)magnitude;
        }
        
        return Task.FromResult<ReadOnlyMemory<float>>(embedding);
    }

    public Task<IReadOnlyList<ReadOnlyMemory<float>>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default)
    {
        var results = new List<ReadOnlyMemory<float>>();
        foreach (var text in texts)
        {
            results.Add(EmbedAsync(text, ct).Result);
        }
        return Task.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(results);
    }
}
