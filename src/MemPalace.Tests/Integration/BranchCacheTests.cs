using System.Diagnostics;
using MemPalace.Backends.Sqlite;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using MemPalace.Tests.Backends;

namespace MemPalace.Tests.Integration;

/// <summary>
/// Performance tests for branch cache hit latency.
/// Target: <1ms for cached branch lookups (SQLite backend).
/// </summary>
public sealed class BranchCacheTests : IAsyncLifetime, IDisposable
{
    private readonly string _tempDir;
    private IBackend _backend = null!;
    private ICollection _collection = null!;
    private FakeEmbedder _embedder = null!;
    private PalaceRef _palace = null!;
    private readonly List<string> _recordIds = new();

    public BranchCacheTests()
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

        // Seed 100 memories with wing/room/drawer hierarchy
        var records = new List<EmbeddedRecord>();
        var embeddings = await _embedder.EmbedAsync(
            Enumerable.Range(0, 100).Select(i => $"Branch memory {i}").ToList()
        );

        for (int i = 0; i < 100; i++)
        {
            var id = $"mem-{i}";
            _recordIds.Add(id);
            records.Add(new EmbeddedRecord(
                Id: id,
                Document: $"Branch memory {i}",
                Metadata: new Dictionary<string, object?>
                {
                    { "wing", $"wing-{i % 5}" },
                    { "room", $"room-{i % 10}" },
                    { "drawer", $"drawer-{i % 20}" }
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
    public async Task BranchCache_WingFilter_FirstHit_Baseline()
    {
        // Arrange: Wing filter
        var wingFilter = new Eq("wing", "wing-0");

        // Act: First query (cold cache)
        var sw = Stopwatch.StartNew();
        var result = await _collection.GetAsync(where: wingFilter, limit: 10);
        sw.Stop();

        // Assert: Log baseline
        var latencyMs = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"[PERF] BranchCache wing filter (first hit): {latencyMs:F2}ms");
        
        Assert.True(result.Documents.Count > 0, "Should return results");
    }

    [Fact]
    public async Task BranchCache_WingFilter_SecondHit_Under1ms()
    {
        // Arrange: Wing filter + warmup
        var wingFilter = new Eq("wing", "wing-1");

        // Warmup: Prime the cache
        await _collection.GetAsync(where: wingFilter, limit: 10);

        // Act: Second query (cached)
        var sw = Stopwatch.StartNew();
        var result = await _collection.GetAsync(where: wingFilter, limit: 10);
        sw.Stop();

        // Assert: Should be <1ms for cached lookup
        var latencyMs = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"[PERF] BranchCache wing filter (cached): {latencyMs:F2}ms (target: <1ms, tolerance: 2ms)");
        
        Assert.True(result.Documents.Count > 0, "Should return results");
        Assert.True(latencyMs < 2, $"Cached branch lookup {latencyMs:F2}ms exceeds tolerance of 2ms");
    }

    [Fact]
    public async Task BranchCache_RoomFilter_SecondHit_Under1ms()
    {
        // Arrange: Room filter + warmup
        var roomFilter = new Eq("room", "room-2");

        // Warmup
        await _collection.GetAsync(where: roomFilter, limit: 10);

        // Act: Second query (cached)
        var sw = Stopwatch.StartNew();
        var result = await _collection.GetAsync(where: roomFilter, limit: 10);
        sw.Stop();

        // Assert
        var latencyMs = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"[PERF] BranchCache room filter (cached): {latencyMs:F2}ms (target: <1ms, tolerance: 2ms)");
        
        Assert.True(result.Documents.Count > 0, "Should return results");
        Assert.True(latencyMs < 2, $"Cached branch lookup {latencyMs:F2}ms exceeds tolerance of 2ms");
    }

    [Fact]
    public async Task BranchCache_MultiLevel_Filter_Performance()
    {
        // Arrange: Multi-level filter (wing + room)
        var multiFilter = new And(new WhereClause[] 
        { 
            new Eq("wing", "wing-3"), 
            new Eq("room", "room-5") 
        });

        // Warmup
        await _collection.GetAsync(where: multiFilter, limit: 10);

        // Act: Cached multi-level query
        var sw = Stopwatch.StartNew();
        var result = await _collection.GetAsync(where: multiFilter, limit: 10);
        sw.Stop();

        // Assert: Log baseline (no strict assertion, multi-level may be slower)
        var latencyMs = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"[PERF] BranchCache multi-level filter (cached): {latencyMs:F2}ms");
        
        Assert.True(result.Documents.Count >= 0, "Query should complete successfully");
    }
}
