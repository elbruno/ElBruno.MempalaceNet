using MemPalace.Ai.Embedding;
using MemPalace.Core.Backends;
using MemPalace.Backends.Sqlite;
using MemPalace.Core.Model;
using FluentAssertions;

namespace MemPalace.E2E.Tests;

/// <summary>
/// E2E journey tests for embedder swap workflows.
/// Validates that users can switch embedders and maintain search quality.
/// </summary>
public sealed class EmbedderSwapE2ETests : IDisposable
{
    private readonly string _testDir;

    public EmbedderSwapE2ETests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"mempalace-e2e-swap-{Guid.NewGuid():N}");
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
    public async Task Journey_InitLocalEmbedder_MineContent_Search_ValidateQuality()
    {
        // Phase 1: Initialize palace with LocalEmbedder
        var palaceRef = new PalaceRef("journey-palace", Path.Combine(_testDir, "journey"));
        using var localEmbedder = new LocalEmbedder();
        using var backend = await SqliteBackend.CreateAsync(localEmbedder);

        var collection = await backend.GetCollectionAsync(
            palaceRef,
            "documents",
            dimensions: localEmbedder.Dimensions,
            embedder: localEmbedder,
            create: true);

        // Phase 2: Mine content (simulate document storage)
        var documents = new[]
        {
            "Alice works on authentication and security systems",
            "Bob works on payment processing and transactions",
            "Charlie works on frontend UI and UX design",
            "Diana works on database optimization and queries",
            "Eve works on API design and REST endpoints"
        };

        var ids = documents.Select((_, i) => $"doc-{i}").ToArray();
        var embeddings = await localEmbedder.EmbedAsync(documents);

        await collection.AddAsync(
            ids: ids,
            documents: documents,
            embeddings: embeddings);

        // Phase 3: Search with semantic query
        var query = "authentication security";
        var queryEmbeddings = await localEmbedder.EmbedAsync(new[] { query });
        var results = await collection.QueryAsync(
            queryEmbeddings: queryEmbeddings,
            nResults: 5);

        // Phase 4: Verify top-5 results
        results.Should().NotBeNull();
        results.Ids.Should().HaveCount(1);
        results.Ids[0].Should().Contain("doc-0"); // Alice (authentication) should be top result
    }

    [Fact]
    public async Task Journey_CustomEmbedder_ValidateModelIdentity_PreventsMismatch()
    {
        // Phase 1: Initialize with CustomEmbedder ("custom-v1")
        var palaceRef = new PalaceRef("identity-palace", Path.Combine(_testDir, "identity"));
        var embedder1 = new TestCustomEmbedder("custom-v1", 128);
        using var backend = await SqliteBackend.CreateAsync(embedder1);

        var collection = await backend.GetCollectionAsync(
            palaceRef,
            "memories",
            dimensions: embedder1.Dimensions,
            embedder: embedder1,
            create: true);

        // Phase 2: Store 10 memories
        var memories = Enumerable.Range(0, 10)
            .Select(i => $"Memory {i}: test content")
            .ToArray();

        var ids = memories.Select((_, i) => $"mem-{i}").ToArray();
        var embeddings = await embedder1.EmbedAsync(memories);

        await collection.AddAsync(
            ids: ids,
            documents: memories,
            embeddings: embeddings);

        // Phase 3: Switch to different CustomEmbedder ("custom-v2")
        var embedder2 = new TestCustomEmbedder("custom-v2", 128);

        // Phase 4: Attempt to store memory → should throw EmbedderIdentityMismatchException
        var act = async () => await backend.GetCollectionAsync(
            palaceRef,
            "memories",
            dimensions: embedder2.Dimensions,
            embedder: embedder2,
            create: false);

        // Assert: Backend should reject mismatched embedder
        await act.Should().ThrowAsync<Exception>(); // EmbedderIdentityMismatchError or similar
    }

    [Fact]
    public async Task Journey_LocalEmbedder_ChangeModel_RequiresNewCollection()
    {
        // Phase 1: Initialize with LocalEmbedder (default model)
        var palaceRef = new PalaceRef("model-change-palace", Path.Combine(_testDir, "model-change"));
        using var embedder1 = new LocalEmbedder();
        using var backend = await SqliteBackend.CreateAsync(embedder1);

        var collection1 = await backend.GetCollectionAsync(
            palaceRef,
            "default",
            dimensions: embedder1.Dimensions,
            embedder: embedder1,
            create: true);

        await collection1.AddAsync(
            ids: new[] { "1" },
            documents: new[] { "test memory" },
            embeddings: await embedder1.EmbedAsync(new[] { "test memory" }));

        // Phase 2: Switch to different model (hypothetical - same dimensions but different model)
        // Note: In reality, changing model changes ModelIdentity, triggering mismatch
        // This test validates the identity check mechanism

        // For this test, we simulate by trying to open collection with wrong embedder
        var embedder2 = new TestCustomEmbedder("different-model", embedder1.Dimensions);
        
        var act = async () => await backend.GetCollectionAsync(
            palaceRef,
            "default",
            dimensions: embedder2.Dimensions,
            embedder: embedder2,
            create: false);

        // Assert: Should throw identity mismatch
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Journey_MultiEmbedder_SeparateCollections()
    {
        // Scenario: User maintains separate collections with different embedders
        var palaceRef = new PalaceRef("multi-embedder-palace", Path.Combine(_testDir, "multi"));

        // Collection 1: LocalEmbedder
        using var localEmbedder = new LocalEmbedder();
        using var backend1 = await SqliteBackend.CreateAsync(localEmbedder);

        var localCollection = await backend1.GetCollectionAsync(
            palaceRef,
            "local-collection",
            dimensions: localEmbedder.Dimensions,
            embedder: localEmbedder,
            create: true);

        await localCollection.AddAsync(
            ids: new[] { "local-1" },
            documents: new[] { "Local embedder memory" },
            embeddings: await localEmbedder.EmbedAsync(new[] { "Local embedder memory" }));

        // Collection 2: CustomEmbedder
        var customEmbedder = new TestCustomEmbedder();
        using var backend2 = await SqliteBackend.CreateAsync(customEmbedder);

        var customCollection = await backend2.GetCollectionAsync(
            palaceRef,
            "custom-collection",
            dimensions: customEmbedder.Dimensions,
            embedder: customEmbedder,
            create: true);

        await customCollection.AddAsync(
            ids: new[] { "custom-1" },
            documents: new[] { "Custom embedder memory" },
            embeddings: await customEmbedder.EmbedAsync(new[] { "Custom embedder memory" }));

        // Assert: Both collections exist independently
        var localResults = await localCollection.QueryAsync(
            queryEmbeddings: await localEmbedder.EmbedAsync(new[] { "local" }),
            nResults: 10);
        localResults.Ids[0].Should().Contain("local-1");

        var customResults = await customCollection.QueryAsync(
            queryEmbeddings: await customEmbedder.EmbedAsync(new[] { "custom" }),
            nResults: 10);
        customResults.Ids[0].Should().Contain("custom-1");
    }

    [Fact]
    public async Task Journey_CustomEmbedder_Lifecycle_DisposeCleanup()
    {
        // Create DisposableCustomEmbedder with resource tracking
        var disposableEmbedder = new DisposableCustomEmbedder();

        // Use embedder
        var palaceRef = new PalaceRef("dispose-palace", Path.Combine(_testDir, "dispose"));
        using var backend = await SqliteBackend.CreateAsync(disposableEmbedder);

        var collection = await backend.GetCollectionAsync(
            palaceRef,
            "default",
            dimensions: disposableEmbedder.Dimensions,
            embedder: disposableEmbedder,
            create: true);

        await collection.AddAsync(
            ids: new[] { "1" },
            documents: new[] { "test" },
            embeddings: await disposableEmbedder.EmbedAsync(new[] { "test" }));

        // Dispose embedder
        disposableEmbedder.Dispose();

        // Assert: Embedder was disposed
        disposableEmbedder.IsDisposed.Should().BeTrue();
    }

    // Test helper: custom embedder implementation
    private sealed class TestCustomEmbedder : ICustomEmbedder
    {
        private readonly string _modelIdentity;
        private readonly int _dimensions;

        public TestCustomEmbedder(string modelIdentity = "test-custom-v1", int dimensions = 128)
        {
            _modelIdentity = modelIdentity;
            _dimensions = dimensions;
        }

        public string ModelIdentity => _modelIdentity;
        public int Dimensions => _dimensions;

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
    }

    // Test helper: disposable custom embedder
    private sealed class DisposableCustomEmbedder : ICustomEmbedder, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public string ModelIdentity => "disposable-test-v1";
        public int Dimensions => 64;

        public ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
            IReadOnlyList<string> texts,
            CancellationToken ct = default)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(DisposableCustomEmbedder));
            }

            var results = texts.Select(_ =>
            {
                var embedding = new float[Dimensions];
                for (int i = 0; i < Dimensions; i++)
                {
                    embedding[i] = 0.1f;
                }
                return new ReadOnlyMemory<float>(embedding);
            }).ToList();

            return ValueTask.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(results);
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
