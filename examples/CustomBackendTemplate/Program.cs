using CustomBackendTemplate;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;

Console.WriteLine("🏛️  MemPalace.NET - Custom Backend Example\n");

// Initialize custom backend (in-memory for demo)
await using var backend = new CustomBackend();

var palace = new PalaceRef("demo-palace");
var embedder = new DemoEmbedder();

// Create a collection
var collection = await backend.GetCollectionAsync(
    palace,
    "documents",
    create: true,
    embedder: embedder);

Console.WriteLine("✓ Created collection with custom backend\n");

// Add some records
Console.WriteLine("📝 Adding records...");
var records = new[]
{
    new EmbeddedRecord(
        Id: "doc-1",
        Document: "Implement authentication system with JWT tokens",
        Metadata: new Dictionary<string, object?> { { "tag", "security" } },
        Embedding: (await embedder.EmbedAsync(new[] { "authentication JWT tokens" }, default))[0]),
    
    new EmbeddedRecord(
        Id: "doc-2",
        Document: "Database optimization and query tuning",
        Metadata: new Dictionary<string, object?> { { "tag", "performance" } },
        Embedding: (await embedder.EmbedAsync(new[] { "database optimization" }, default))[0]),
    
    new EmbeddedRecord(
        Id: "doc-3",
        Document: "API design using REST principles",
        Metadata: new Dictionary<string, object?> { { "tag", "design" } },
        Embedding: (await embedder.EmbedAsync(new[] { "API REST design" }, default))[0])
};

await collection.UpsertAsync(records);
Console.WriteLine($"✓ Added {records.Length} records\n");

// Search
Console.WriteLine("🔍 Searching for 'security features'...");
var queryEmbedding = (await embedder.EmbedAsync(new[] { "security features" }, default))[0];
var results = await collection.QueryAsync(
    new[] { queryEmbedding },
    nResults: 2,
    include: IncludeFields.Documents | IncludeFields.Distances | IncludeFields.Metadatas);

Console.WriteLine($"Found {results.Ids[0].Count} results:\n");
for (int i = 0; i < results.Ids[0].Count; i++)
{
    Console.WriteLine($"  [{i + 1}] {results.Documents[0][i]}");
    Console.WriteLine($"      Distance: {results.Distances[0][i]:F3}");
    Console.WriteLine();
}

// Count
var count = await collection.CountAsync();
Console.WriteLine($"📊 Total records in collection: {count}\n");

// List collections
var collections = await backend.ListCollectionsAsync(palace);
Console.WriteLine($"📚 Collections in palace: {string.Join(", ", collections)}");

Console.WriteLine("\n✅ Custom backend example completed!");

// Simple demo embedder for testing
internal class DemoEmbedder : IEmbedder
{
    public int Dimensions => 128;
    public string ModelIdentity => "demo-embedder-v1";

    public async ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default)
    {
        var results = new List<ReadOnlyMemory<float>>();

        foreach (var text in texts)
        {
            var embedding = new float[Dimensions];
            var hash = text.GetHashCode();
            var random = new Random(hash);

            for (int i = 0; i < Dimensions; i++)
                embedding[i] = (float)(random.NextDouble() * 2.0 - 1.0);

            // Normalize
            var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
            for (int i = 0; i < Dimensions; i++)
                embedding[i] /= magnitude;

            results.Add(embedding);
        }

        return await ValueTask.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(results);
    }
}
