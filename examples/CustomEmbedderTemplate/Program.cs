using CustomEmbedderTemplate;
using MemPalace.Core.Backends;
using MemPalace.Core.Backends.InMemory;
using MemPalace.Core.Model;

Console.WriteLine("🏛️  MemPalace.NET - Custom Embedder Example\n");

// Initialize in-memory backend with custom embedder
await using var backend = new InMemoryBackend();

var palace = new PalaceRef("demo-palace");
var embedder = new CustomEmbedder();

// Create a collection
var collection = await backend.GetCollectionAsync(
    palace,
    "documents",
    create: true,
    embedder: embedder);

Console.WriteLine($"✓ Created collection with custom embedder: {embedder.ModelIdentity}");
Console.WriteLine($"  Embedding dimensions: {embedder.Dimensions}\n");

// Add records with custom embeddings
Console.WriteLine("📝 Adding records with custom embeddings...");

var records = new[]
{
    new EmbeddedRecord(
        Id: "doc-1",
        Document: "Neural networks are inspired by biological neurons",
        Metadata: new Dictionary<string, object?> { { "topic", "deep-learning" } },
        Embedding: (await embedder.EmbedAsync(new[] { "neural networks" }, default))[0]),
    
    new EmbeddedRecord(
        Id: "doc-2",
        Document: "Transformers revolutionized natural language processing",
        Metadata: new Dictionary<string, object?> { { "topic", "nlp" } },
        Embedding: (await embedder.EmbedAsync(new[] { "transformers nlp" }, default))[0]),
    
    new EmbeddedRecord(
        Id: "doc-3",
        Document: "Vector embeddings enable semantic similarity search",
        Metadata: new Dictionary<string, object?> { { "topic", "embeddings" } },
        Embedding: (await embedder.EmbedAsync(new[] { "vector embeddings semantic" }, default))[0])
};

await collection.UpsertAsync(records);
Console.WriteLine($"✓ Added {records.Length} records\n");

// Demonstrate embedder consistency
Console.WriteLine("🔍 Testing embedder consistency...");
var text1 = "machine learning";
var embed1_a = (await embedder.EmbedAsync(new[] { text1 }, default))[0];
var embed1_b = (await embedder.EmbedAsync(new[] { text1 }, default))[0];

// Same text should produce identical embeddings
var identical = CompareEmbeddings(embed1_a, embed1_b);
Console.WriteLine($"✓ Same text produces identical embeddings: {identical}\n");

// Search using custom embeddings
Console.WriteLine("🔍 Searching with custom embedder...");
var query = "machine learning algorithms";
var queryEmbedding = (await embedder.EmbedAsync(new[] { query }, default))[0];

var results = await collection.QueryAsync(
    new[] { queryEmbedding },
    nResults: 3,
    include: IncludeFields.Documents | IncludeFields.Distances | IncludeFields.Metadatas);

Console.WriteLine($"Results for '{query}':\n");
for (int i = 0; i < results.Ids[0].Count; i++)
{
    var similarity = 1f - results.Distances[0][i];  // Invert distance to similarity
    Console.WriteLine($"  [{i + 1}] (similarity: {similarity:F3})");
    Console.WriteLine($"      {results.Documents[0][i]}\n");
}

Console.WriteLine("✅ Custom embedder example completed!");

// Helper function to compare embeddings
static bool CompareEmbeddings(ReadOnlyMemory<float> a, ReadOnlyMemory<float> b)
{
    var aSpan = a.Span;
    var bSpan = b.Span;
    
    if (aSpan.Length != bSpan.Length) return false;
    
    for (int i = 0; i < aSpan.Length; i++)
    {
        if (!aSpan[i].Equals(bSpan[i])) return false;
    }
    
    return true;
}
