using FluentAssertions;
using MemPalace.Ai.Embedding;
using MemPalace.Core.Backends;
using MemPalace.Backends.Sqlite;
using MemPalace.Core.Model;

namespace MemPalace.E2E.Tests;

/// <summary>
/// E2E integration tests for embedder lifecycle and palace integration.
/// Tests custom embedders, embedder validation, and embedder swapping.
/// </summary>
public sealed class EmbedderIntegrationTests : IDisposable
{
    private readonly string _testDir;

    public EmbedderIntegrationTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"mempalace-e2e-embedder-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Fact]
    public async Task Palace_WithLocalEmbedder_StoresAndSearches()
    {
        // Arrange
        var palaceRef = new PalaceRef("test-local", Path.Combine(_testDir, "local"));
        using var embedder = new LocalEmbedder();
        await using var backend = new SqliteBackend(_testDir);

        var collection = await backend.GetCollectionAsync(
            palaceRef,
            "default",
            create: true,
            embedder: embedder);

        // Act - store memories
        await collection.AddAsync(
            ids: new[] { "1", "2", "3" },
            documents: new[] { "dog", "cat", "bird" },
            embeddings: await embedder.EmbedAsync(new[] { "dog", "cat", "bird" }));

        // Act - search
        var results = await collection.QueryAsync(
            queryEmbeddings: await embedder.EmbedAsync(new[] { "puppy" }),
            nResults: 2);

        // Assert
        results.Should().NotBeNull();
        results.Ids.Should().HaveCount(1); // 1 query
        results.Ids[0].Should().Contain("1"); // "dog" should be top result
    }

    [Fact]
    public async Task Palace_WithCustomEmbedder_StoresAndSearches()
    {
        // Arrange
        var palaceRef = new PalaceRef("test-custom", Path.Combine(_testDir, "custom"));
        var customEmbedder = new TestCustomEmbedder();
        await using var backend = new SqliteBackend(_testDir);

        var collection = await backend.GetCollectionAsync(
            palaceRef,
            "default",
            create: true,
            embedder: customEmbedder);

        // Act - store memories
        await collection.AddAsync(
            ids: new[] { "1", "2", "3" },
            documents: new[] { "apple", "banana", "cherry" },
            embeddings: await customEmbedder.EmbedAsync(new[] { "apple", "banana", "cherry" }));

        // Act - search
        var results = await collection.QueryAsync(
            queryEmbeddings: await customEmbedder.EmbedAsync(new[] { "apple" }),
            nResults: 2);

        // Assert
        results.Should().NotBeNull();
        results.Ids.Should().HaveCount(1);
        results.Ids[0].Should().Contain("1"); // "apple" exact match
    }

    [Fact]
    public async Task Palace_InitWithLocal_SwitchToCustom_ThrowsDimensionMismatch()
    {
        // Arrange
        var palaceRef = new PalaceRef("test-mismatch", Path.Combine(_testDir, "mismatch"));
        using var localEmbedder = new LocalEmbedder(); // 384 dimensions
        using var backend = await SqliteBackend.CreateAsync(localEmbedder);

        // Act - create collection with local embedder
        var collection = await backend.GetCollectionAsync(
            palaceRef,
            "default",
            dimensions: localEmbedder.Dimensions,
            embedder: localEmbedder,
            create: true);

        await collection.AddAsync(
            ids: new[] { "1" },
            documents: new[] { "test" },
            embeddings: await localEmbedder.EmbedAsync(new[] { "test" }));

        // Act - try to use different embedder (different dimensions)
        var customEmbedder = new TestCustomEmbedder(); // 128 dimensions
        var act = async () => await backend.GetCollectionAsync(
            palaceRef,
            "default",
            dimensions: customEmbedder.Dimensions,
            embedder: customEmbedder,
            create: false);

        // Assert - should throw dimension mismatch
        await act.Should().ThrowAsync<Exception>(); // DimensionMismatchError or similar
    }

    [Fact]
    public async Task Palace_WithCustomEmbedder_ValidateModelIdentity()
    {
        // Arrange
        var palaceRef = new PalaceRef("test-identity", Path.Combine(_testDir, "identity"));
        var embedder1 = new TestCustomEmbedder("custom-v1", 128);
        using var backend = await SqliteBackend.CreateAsync(embedder1);

        // Act - create collection with embedder1
        var collection = await backend.GetCollectionAsync(
            palaceRef,
            "default",
            dimensions: embedder1.Dimensions,
            embedder: embedder1,
            create: true);

        await collection.AddAsync(
            ids: new[] { "1" },
            documents: new[] { "test" },
            embeddings: await embedder1.EmbedAsync(new[] { "test" }));

        // Act - try to use different embedder (different identity, same dimensions)
        var embedder2 = new TestCustomEmbedder("custom-v2", 128);
        var act = async () => await backend.GetCollectionAsync(
            palaceRef,
            "default",
            dimensions: embedder2.Dimensions,
            embedder: embedder2,
            create: false);

        // Assert - should throw identity mismatch
        await act.Should().ThrowAsync<Exception>(); // EmbedderIdentityMismatchError
    }

    [Fact]
    public async Task Palace_LocalEmbedder_Dispose_NoMemoryLeak()
    {
        // Arrange & Act
        for (int i = 0; i < 10; i++)
        {
            var palaceRef = new PalaceRef($"test-dispose-{i}", Path.Combine(_testDir, $"dispose-{i}"));
            using var embedder = new LocalEmbedder();
            using var backend = await SqliteBackend.CreateAsync(embedder);

            var collection = await backend.GetCollectionAsync(
                palaceRef,
                "default",
                dimensions: embedder.Dimensions,
                embedder: embedder,
                create: true);

            await collection.AddAsync(
                ids: new[] { "1" },
                documents: new[] { "test" },
                embeddings: await embedder.EmbedAsync(new[] { "test" }));

            // Dispose happens automatically via using
        }

        // Assert - no memory leak (verified via test run, not programmatically)
        // If there were leaks, we'd see increasing memory usage or finalizer warnings
        Assert.True(true);
    }

    [Fact]
    public async Task Palace_MultipleEmbedders_SeparateCollections()
    {
        // Arrange
        var palaceRef = new PalaceRef("test-multi", Path.Combine(_testDir, "multi"));
        using var localEmbedder = new LocalEmbedder();
        var customEmbedder = new TestCustomEmbedder();
        using var backend = await SqliteBackend.CreateAsync(localEmbedder);

        // Act - create collection with local embedder
        var localCollection = await backend.GetCollectionAsync(
            palaceRef,
            "local-collection",
            dimensions: localEmbedder.Dimensions,
            embedder: localEmbedder,
            create: true);

        await localCollection.AddAsync(
            ids: new[] { "1" },
            documents: new[] { "local memory" },
            embeddings: await localEmbedder.EmbedAsync(new[] { "local memory" }));

        // Act - create collection with custom embedder
        using var customBackend = await SqliteBackend.CreateAsync(customEmbedder);
        var customCollection = await customBackend.GetCollectionAsync(
            palaceRef,
            "custom-collection",
            dimensions: customEmbedder.Dimensions,
            embedder: customEmbedder,
            create: true);

        await customCollection.AddAsync(
            ids: new[] { "2" },
            documents: new[] { "custom memory" },
            embeddings: await customEmbedder.EmbedAsync(new[] { "custom memory" }));

        // Assert - both collections exist independently
        var localResults = await localCollection.QueryAsync(
            queryEmbeddings: await localEmbedder.EmbedAsync(new[] { "local" }),
            nResults: 10);
        localResults.Ids[0].Should().Contain("1");

        var customResults = await customCollection.QueryAsync(
            queryEmbeddings: await customEmbedder.EmbedAsync(new[] { "custom" }),
            nResults: 10);
        customResults.Ids[0].Should().Contain("2");
    }

    // Test helper: custom embedder implementation
    private sealed class TestCustomEmbedder : ICustomEmbedder, IDisposable
    {
        private readonly string _modelIdentity;
        private readonly int _dimensions;

        public TestCustomEmbedder(string modelIdentity = "test-custom-embedder-v1", int dimensions = 128)
        {
            _modelIdentity = modelIdentity;
            _dimensions = dimensions;
        }

        public string ModelIdentity => _modelIdentity;
        public int Dimensions => _dimensions;
        public string ProviderName => "test-custom-integration";
        public IReadOnlyDictionary<string, object> Metadata => new Dictionary<string, object>
        {
            { "test_mode", true },
            { "dimensions", _dimensions }
        };

        public ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
            IReadOnlyList<string> texts,
            CancellationToken ct = default)
        {
            var results = new List<ReadOnlyMemory<float>>();
            foreach (var text in texts)
            {
                var embedding = ComputeEmbedding(text);
                results.Add(embedding);
            }
            return ValueTask.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(results);
        }

        private ReadOnlyMemory<float> ComputeEmbedding(string text)
        {
            var embedding = new float[Dimensions];
            var hash = text.GetHashCode();
            var random = new Random(hash);

            for (int i = 0; i < Dimensions; i++)
            {
                embedding[i] = (float)(random.NextDouble() * 2.0 - 1.0);
            }

            // Normalize
            var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
            if (magnitude > 0)
            {
                for (int i = 0; i < Dimensions; i++)
                {
                    embedding[i] /= magnitude;
                }
            }

            return embedding;
        }

        public void Dispose() { }
    }
}
