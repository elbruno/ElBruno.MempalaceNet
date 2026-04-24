using BenchmarkDotNet.Attributes;
using MemPalace.Benchmarks.Core;
using MemPalace.Core.Backends;
using MemPalace.Core.Backends.InMemory;
using MemPalace.Core.Model;

namespace MemPalace.Benchmarks.Micro;

/// <summary>
/// Micro-benchmarks for vector query latency.
/// </summary>
[MemoryDiagnoser]
public class VectorQueryLatencyBench
{
    private IBackend? _backend;
    private ICollection? _collection;
    private ReadOnlyMemory<float> _queryVector;
    private const int Dimensions = 384;

    [Params(1000, 10000)]
    public int VectorCount { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        _backend = new InMemoryBackend();
        var embedder = new DeterministicEmbedder(Dimensions);
        
        var palace = new PalaceRef(
            Id: "bench",
            LocalPath: Environment.CurrentDirectory,
            Namespace: "bench");

        _collection = await _backend.GetCollectionAsync(palace, "bench", create: true, embedder);

        // Insert vectors
        var records = new List<EmbeddedRecord>();
        for (var i = 0; i < VectorCount; i++)
        {
            var text = $"Document {i}";
            var embedding = await embedder.EmbedAsync(new[] { text });
            records.Add(new EmbeddedRecord(
                Id: $"doc-{i}",
                Document: text,
                Metadata: new Dictionary<string, object?> { ["index"] = i },
                Embedding: embedding[0]));
        }

        await _collection.UpsertAsync(records);

        // Prepare query vector
        var queryEmbed = await embedder.EmbedAsync(new[] { "Query document" });
        _queryVector = queryEmbed[0];
    }

    [Benchmark]
    public async Task<QueryResult> QueryTop10()
    {
        if (_collection == null)
            throw new InvalidOperationException("Collection not initialized");

        return await _collection.QueryAsync(new[] { _queryVector }, 10);
    }
}
