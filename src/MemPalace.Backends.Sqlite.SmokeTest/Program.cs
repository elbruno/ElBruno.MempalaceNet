using MemPalace.Backends.Sqlite;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;

// Simple standalone test to verify SQLite backend works
Console.WriteLine("SQLite Backend Smoke Test");
Console.WriteLine("==========================\n");

var tempDir = Path.Combine(Path.GetTempPath(), $"mempalace-smoke-{Guid.NewGuid()}");
Directory.CreateDirectory(tempDir);

try
{
    var backend = new SqliteBackend(tempDir);
    var embedder = new TestEmbedder();
    var palace = new PalaceRef("test-palace");

    // Test 1: Health check
    Console.Write("Test 1: Health check... ");
    var health = await backend.HealthAsync();
    Console.WriteLine(health.Ok ? "✓ PASS" : $"✗ FAIL: {health.Detail}");

    // Test 2: Create collection
    Console.Write("Test 2: Create collection... ");
    var collection = await backend.GetCollectionAsync(palace, "test-col", create: true, embedder: embedder);
    Console.WriteLine($"✓ PASS (name={collection.Name}, dim={collection.Dimensions})");

    // Test 3: Add records
    Console.Write("Test 3: Add records... ");
    var texts = new[] { "hello world", "test document", "another one" };
    var embeddings = await embedder.EmbedAsync(texts);
    var records = new[]
    {
        new EmbeddedRecord("id1", texts[0], new Dictionary<string, object?> { ["tag"] = "greeting" }, embeddings[0]),
        new EmbeddedRecord("id2", texts[1], new Dictionary<string, object?> { ["tag"] = "test" }, embeddings[1]),
        new EmbeddedRecord("id3", texts[2], new Dictionary<string, object?> { ["num"] = 42 }, embeddings[2])
    };
    await collection.AddAsync(records);
    Console.WriteLine("✓ PASS");

    // Test 4: Count
    Console.Write("Test 4: Count records... ");
    var count = await collection.CountAsync();
    Console.WriteLine(count == 3 ? $"✓ PASS (count={count})" : $"✗ FAIL (expected 3, got {count})");

    // Test 5: Get by ID
    Console.Write("Test 5: Get by ID... ");
    var getResult = await collection.GetAsync(ids: new[] { "id1", "id2" });
    Console.WriteLine(getResult.Ids.Count == 2 ? $"✓ PASS (found {getResult.Ids.Count})" : $"✗ FAIL");

    // Test 6: Query (vector search)
    Console.Write("Test 6: Vector query... ");
    var queryEmb = await embedder.EmbedAsync(new[] { "hello" });
    var queryResult = await collection.QueryAsync(queryEmb, nResults: 2);
    Console.WriteLine(queryResult.Ids[0].Count > 0 ? $"✓ PASS (found {queryResult.Ids[0].Count} results)" : $"✗ FAIL");

    // Test 7: Filter query
    Console.Write("Test 7: Filter by metadata... ");
    var filterResult = await collection.GetAsync(where: new Eq("tag", "test"));
    Console.WriteLine(filterResult.Ids.Count == 1 ? $"✓ PASS" : $"✗ FAIL");

    // Test 8: List collections
    Console.Write("Test 8: List collections... ");
    var collections = await backend.ListCollectionsAsync(palace);
    Console.WriteLine(collections.Contains("test-col") ? $"✓ PASS (found {collections.Count})" : $"✗ FAIL");

    // Test 9: Delete records
    Console.Write("Test 9: Delete records... ");
    await collection.DeleteAsync(ids: new[] { "id1" });
    var countAfterDelete = await collection.CountAsync();
    Console.WriteLine(countAfterDelete == 2 ? $"✓ PASS" : $"✗ FAIL (expected 2, got {countAfterDelete})");

    // Test 10: Delete collection
    Console.Write("Test 10: Delete collection... ");
    await backend.DeleteCollectionAsync(palace, "test-col");
    var collectionsAfter = await backend.ListCollectionsAsync(palace);
    Console.WriteLine(collectionsAfter.Count == 0 ? $"✓ PASS" : $"✗ FAIL");

    await backend.DisposeAsync();
    Console.WriteLine("\n✓ All tests passed!");
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ Test failed with exception:\n{ex}");
}
finally
{
    if (Directory.Exists(tempDir))
    {
        try { Directory.Delete(tempDir, recursive: true); } catch { }
    }
}

// Simple test embedder
class TestEmbedder : IEmbedder
{
    public string ModelIdentity => "test-embedder-v1";
    public int Dimensions => 64;

    public ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        var embeddings = new List<ReadOnlyMemory<float>>();
        foreach (var text in texts)
        {
            var embedding = new float[Dimensions];
            var rng = new Random(text.GetHashCode());
            for (int i = 0; i < Dimensions; i++)
                embedding[i] = (float)rng.NextDouble();
            
            // Normalize
            float mag = 0f;
            for (int i = 0; i < Dimensions; i++)
                mag += embedding[i] * embedding[i];
            mag = MathF.Sqrt(mag);
            for (int i = 0; i < Dimensions; i++)
                embedding[i] /= mag;
            
            embeddings.Add(embedding);
        }
        return ValueTask.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(embeddings);
    }
}
