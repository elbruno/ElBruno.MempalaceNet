using System.Diagnostics;
using MemPalace.Backends.Sqlite;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using MemPalace.Tests.Backends;

namespace MemPalace.Tests.Integration;

/// <summary>
/// Performance tests for Delete and Filter operations.
/// Tests deletion throughput, filtered deletes, and post-delete query consistency.
/// </summary>
public sealed class DeleteFilterTests : IAsyncLifetime, IDisposable
{
    private readonly string _tempDir;
    private IBackend _backend = null!;
    private ICollection _collection = null!;
    private FakeEmbedder _embedder = null!;
    private PalaceRef _palace = null!;
    private readonly List<string> _recordIds = new();

    public DeleteFilterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"mempalace-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public async Task InitializeAsync()
    {
        _backend = new SqliteBackend(_tempDir);
        _embedder = new FakeEmbedder();
        _palace = new PalaceRef($"test-palace-{Guid.NewGuid()}");
        _collection = await _backend.GetCollectionAsync(_palace, "test-collection", create: true, embedder: _embedder);

        // Seed 500 memories
        var records = new List<EmbeddedRecord>();
        var embeddings = await _embedder.EmbedAsync(
            Enumerable.Range(0, 500).Select(i => $"Delete test memory {i}").ToList()
        );

        for (int i = 0; i < 500; i++)
        {
            var id = $"mem-{i}";
            _recordIds.Add(id);
            records.Add(new EmbeddedRecord(
                Id: id,
                Document: $"Delete test memory {i}",
                Metadata: new Dictionary<string, object?>
                {
                    { "category", i % 2 == 0 ? "keep" : "delete" },
                    { "timestamp", DateTimeOffset.UtcNow.AddMinutes(-i).ToUnixTimeSeconds() }
                },
                Embedding: embeddings[i]
            ));
        }

        await _collection.AddAsync(records);
    }

    public async Task DisposeAsync()
    {
        if (_collection != null)
            await _collection.DisposeAsync();
        if (_backend != null)
            await _backend.DisposeAsync();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    [Fact]
    public async Task Delete_ById_Batch50_Performance()
    {
        // Arrange: Select 50 IDs to delete
        var idsToDelete = _recordIds.Take(50).ToList();

        // Act: Measure batch delete latency
        var sw = Stopwatch.StartNew();
        await _collection.DeleteAsync(ids: idsToDelete);
        sw.Stop();

        // Assert: Log performance and verify deletion
        var latencyMs = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"[PERF] Delete batch (50 records): {latencyMs:F2}ms");

        var remainingCount = await _collection.CountAsync();
        Assert.Equal(450, remainingCount);
    }

    [Fact]
    public async Task Delete_ByFilter_HalfCollection_Performance()
    {
        // Arrange: Filter for "delete" category (250 records)
        var deleteFilter = new Eq("category", "delete");

        // Act: Measure filtered delete latency
        var sw = Stopwatch.StartNew();
        await _collection.DeleteAsync(where: deleteFilter);
        sw.Stop();

        // Assert: Log performance and verify deletion
        var latencyMs = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"[PERF] Delete by filter (250 records): {latencyMs:F2}ms");

        var remainingCount = await _collection.CountAsync();
        Assert.Equal(250, remainingCount);
    }

    [Fact]
    public async Task Delete_And_Query_Consistency()
    {
        // Arrange: Delete a subset
        var idsToDelete = _recordIds.Take(10).ToList();
        await _collection.DeleteAsync(ids: idsToDelete);

        // Act: Query should not return deleted records
        var queryEmbedding = await _embedder.EmbedAsync(new[] { "test query" });
        var result = await _collection.QueryAsync(queryEmbedding, nResults: 20);

        // Assert: Verify no deleted IDs are returned
        var returnedIds = result.Ids[0];
        Assert.DoesNotContain(idsToDelete, id => returnedIds.Contains(id));
        Console.WriteLine($"[PERF] Delete+Query consistency verified: {result.Ids[0].Count} results returned");
    }

    [Fact]
    public async Task Filter_Get_Metadata_Performance()
    {
        // Arrange: Metadata filter for "keep" category
        var keepFilter = new Eq("category", "keep");

        // Warmup
        await _collection.GetAsync(where: keepFilter, limit: 50);

        // Act: Measure cached filter performance
        var sw = Stopwatch.StartNew();
        var result = await _collection.GetAsync(where: keepFilter, limit: 50);
        sw.Stop();

        // Assert: Log performance
        var latencyMs = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"[PERF] Filter Get (metadata, cached): {latencyMs:F2}ms");

        Assert.True(result.Documents.Count > 0, "Should return filtered results");
    }

    [Fact]
    public async Task Filter_And_Delete_CombinedWorkflow_Performance()
    {
        // Arrange: Multi-step workflow (filter → delete → verify)
        var oldMemoriesFilter = new Lt("timestamp", DateTimeOffset.UtcNow.AddMinutes(-400).ToUnixTimeSeconds());

        // Act: Measure combined workflow
        var sw = Stopwatch.StartNew();
        
        // Step 1: Filter to find old memories
        var oldRecords = await _collection.GetAsync(where: oldMemoriesFilter, limit: 1000);
        var oldCount = oldRecords.Documents.Count;
        
        // Step 2: Delete old memories
        await _collection.DeleteAsync(where: oldMemoriesFilter);
        
        // Step 3: Verify deletion
        var remainingCount = await _collection.CountAsync();
        
        sw.Stop();

        // Assert: Log performance
        var latencyMs = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"[PERF] Filter+Delete workflow (deleted {oldCount} records): {latencyMs:F2}ms");

        Assert.True(remainingCount < 500, "Should have deleted old records");
    }
}
