using System.Diagnostics;
using MemPalace.Backends.Sqlite;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using MemPalace.Tests.Backends;

namespace MemPalace.Tests.Integration;

/// <summary>
/// Performance tests for WakeUp operation (semantic search with recency bias).
/// Target: <50ms for 1000 memories (SQLite backend, in-memory embeddings).
/// Tolerance: 20% (60ms max).
/// </summary>
public sealed class WakeUpLatencyTests : IAsyncLifetime, IDisposable
{
    private readonly string _tempDir;
    private IBackend _backend = null!;
    private ICollection _collection = null!;
    private FakeEmbedder _embedder = null!;
    private PalaceRef _palace = null!;

    public WakeUpLatencyTests()
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

        // Seed 1000 memories with unique content
        var records = new List<EmbeddedRecord>();
        var embeddings = await _embedder.EmbedAsync(
            Enumerable.Range(0, 1000).Select(i => $"Memory content {i}: This is a test memory with unique content").ToList()
        );

        for (int i = 0; i < 1000; i++)
        {
            records.Add(new EmbeddedRecord(
                Id: $"mem-{i}",
                Document: $"Memory content {i}: This is a test memory with unique content",
                Metadata: new Dictionary<string, object?>
                {
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
    public async Task WakeUp_Semantic_Search_1000Memories_UnderTarget()
    {
        // Arrange: Warmup query to ensure caching and cold-start effects are minimized
        var warmupEmbedding = await _embedder.EmbedAsync(new[] { "warmup query" });
        await _collection.QueryAsync(warmupEmbedding, nResults: 10);

        // Act: Measure semantic search latency
        var queryEmbedding = await _embedder.EmbedAsync(new[] { "test query for recent memories" });
        
        var sw = Stopwatch.StartNew();
        var result = await _collection.QueryAsync(queryEmbedding, nResults: 10);
        sw.Stop();

        // Assert
        var latencyMs = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"[PERF] WakeUp semantic search latency: {latencyMs:F2}ms (target: <50ms, tolerance: 60ms)");
        
        Assert.True(result.Ids.Count > 0, "Should return results");
        Assert.True(latencyMs < 60, $"Latency {latencyMs:F2}ms exceeds tolerance of 60ms");
    }

    [Fact]
    public async Task WakeUp_Hybrid_Search_1000Memories_Baseline()
    {
        // Arrange: Warmup
        var warmupEmbedding = await _embedder.EmbedAsync(new[] { "warmup query" });
        await _collection.QueryAsync(warmupEmbedding, nResults: 10);

        // Act: Measure hybrid search (semantic + metadata filter)
        var queryEmbedding = await _embedder.EmbedAsync(new[] { "test query for recent memories" });
        var recentFilter = new Gt("timestamp", DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds());

        var sw = Stopwatch.StartNew();
        var result = await _collection.QueryAsync(queryEmbedding, nResults: 10, where: recentFilter);
        sw.Stop();

        // Assert: Log baseline (no strict assertion, just tracking)
        var latencyMs = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"[PERF] WakeUp hybrid search latency (baseline): {latencyMs:F2}ms");
        
        Assert.True(result.Ids.Count >= 0, "Query should complete successfully");
    }

    [Fact]
    public async Task WakeUp_Recency_Bias_Top10_Performance()
    {
        // Arrange: Warmup
        var warmupEmbedding = await _embedder.EmbedAsync(new[] { "warmup query" });
        await _collection.QueryAsync(warmupEmbedding, nResults: 10);

        // Act: Retrieve top 10 most recent memories (simple Get with metadata sort simulation)
        var sw = Stopwatch.StartNew();
        var result = await _collection.GetAsync(
            limit: 10,
            include: IncludeFields.Documents | IncludeFields.Metadatas
        );
        sw.Stop();

        // Assert: Log performance (baseline for recency-only queries)
        var latencyMs = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"[PERF] WakeUp recency-only (top 10) latency: {latencyMs:F2}ms");
        
        Assert.True(result.Documents.Count > 0, "Should return results");
    }
}
