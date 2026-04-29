using FluentAssertions;
using MemPalace.Search;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using NSubstitute;
using MemPalace.Tests.Search.Fixtures;

namespace MemPalace.Tests.Search;

/// <summary>
/// Tests for HybridSearchService with BM25 integration focus.
/// Verifies backward compatibility with existing token-overlap implementation.
/// Validates fusion of vector and keyword signals.
/// </summary>
public sealed class HybridSearchWithBM25Tests
{
    [Fact]
    public async Task SearchAsync_BackwardCompatibility_ExistingTests_StillPass()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var embedder = Substitute.For<IEmbedder>();
        var collection = Substitute.For<ICollection>();

        embedder.ModelIdentity.Returns("test-model");
        embedder.Dimensions.Returns(128);
        embedder.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new ReadOnlyMemory<float>(new float[128]) });

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>()
        ).Returns(collection);

        var queryResult = SearchTestData.CreateMockQueryResult(
            ids: new[] { "doc1", "doc2", "doc3" },
            documents: new[] { "doc1", "doc2", "doc3" },
            distances: new[] { 0.1f, 0.2f, 0.3f });

        collection.QueryAsync(
            Arg.Any<IReadOnlyList<ReadOnlyMemory<float>>>(),
            Arg.Any<int>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(queryResult);

        var service = new HybridSearchService(backend, embedder);

        // Act
        var results = await service.SearchAsync(
            query: "machine learning algorithms",
            collection: "test-collection",
            opts: new SearchOptions(TopK: 10),
            ct: CancellationToken.None);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(r => r.Score.Should().BeGreaterThan(0));
        results.Should().AllSatisfy(r => r.Metadata?.Should().ContainKey("sources"));
    }

    [Fact]
    public async Task SearchAsync_VectorSearchComponentWorks()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var embedder = Substitute.For<IEmbedder>();
        var collection = Substitute.For<ICollection>();

        embedder.ModelIdentity.Returns("test-model");
        embedder.Dimensions.Returns(128);
        embedder.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new ReadOnlyMemory<float>(new float[128]) });

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>()
        ).Returns(collection);

        var queryResult = SearchTestData.CreateMockQueryResult(
            ids: new[] { "v-1", "v-2", "v-3" },
            documents: new[] { "semantic match 1", "semantic match 2", "semantic match 3" },
            distances: new[] { 0.05f, 0.10f, 0.15f });

        collection.QueryAsync(
            Arg.Any<IReadOnlyList<ReadOnlyMemory<float>>>(),
            Arg.Any<int>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(queryResult);

        var service = new HybridSearchService(backend, embedder);

        // Act
        var results = await service.SearchAsync(
            query: "semantic search",
            collection: "test-collection",
            opts: new SearchOptions(TopK: 3),
            ct: CancellationToken.None);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().HaveCountLessThanOrEqualTo(3);
        await embedder.Received().EmbedAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_KeywordComponentWorks_ViaTokenOverlap()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var embedder = Substitute.For<IEmbedder>();
        var collection = Substitute.For<ICollection>();

        embedder.ModelIdentity.Returns("test-model");
        embedder.Dimensions.Returns(128);
        embedder.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new ReadOnlyMemory<float>(new float[128]) });

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>()
        ).Returns(collection);

        var queryResult = SearchTestData.CreateMockQueryResult(
            ids: new[] { "k-1", "k-2", "k-3" },
            documents: new[]
            {
                "authentication bcrypt password hashing security",
                "OAuth federated authentication identity",
                "machine learning neural networks"
            },
            distances: new[] { 0.2f, 0.3f, 0.4f });

        collection.QueryAsync(
            Arg.Any<IReadOnlyList<ReadOnlyMemory<float>>>(),
            Arg.Any<int>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(queryResult);

        var service = new HybridSearchService(backend, embedder);

        // Act
        var results = await service.SearchAsync(
            query: "authentication password",
            collection: "test-collection",
            opts: new SearchOptions(TopK: 3),
            ct: CancellationToken.None);

        // Assert
        results.Should().NotBeEmpty();
        results[0].Document.Should().Contain("authentication");
    }

    [Fact]
    public async Task SearchAsync_RRFFusionCombinesBothSignals()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var embedder = Substitute.For<IEmbedder>();
        var collection = Substitute.For<ICollection>();

        embedder.ModelIdentity.Returns("test-model");
        embedder.Dimensions.Returns(128);
        embedder.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new ReadOnlyMemory<float>(new float[128]) });

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>()
        ).Returns(collection);

        var queryResult = SearchTestData.CreateMockQueryResult(
            ids: new[] { "v-best-k-medium", "v-medium-k-best", "v-worst-k-worst" },
            documents: new[]
            {
                "semantic vector match machine learning",
                "exact match learning algorithms",
                "unrelated content here"
            },
            distances: new[] { 0.05f, 0.15f, 0.5f });

        collection.QueryAsync(
            Arg.Any<IReadOnlyList<ReadOnlyMemory<float>>>(),
            Arg.Any<int>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(queryResult);

        var service = new HybridSearchService(backend, embedder);

        // Act
        var results = await service.SearchAsync(
            query: "machine learning",
            collection: "test-collection",
            opts: new SearchOptions(TopK: 3),
            ct: CancellationToken.None);

        // Assert
        results.Should().NotBeEmpty();
        results.Count.Should().BeLessThanOrEqualTo(3);
        results.First().Score.Should().BeGreaterThan(results.Last().Score);
    }

    [Fact]
    public async Task SearchAsync_AppliesWingFilter()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var embedder = Substitute.For<IEmbedder>();
        var collection = Substitute.For<ICollection>();

        embedder.ModelIdentity.Returns("test-model");
        embedder.Dimensions.Returns(128);
        embedder.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new ReadOnlyMemory<float>(new float[128]) });

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>()
        ).Returns(collection);

        var metadatas = new[]
        {
            (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?> { { "wing", "technical" } },
            (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?> { { "wing", "personal" } }
        };

        var queryResult = SearchTestData.CreateMockQueryResult(
            ids: new[] { "tech-1", "personal-1" },
            documents: new[]
            {
                SearchTestData.TechnicalMemory1,
                SearchTestData.ConversationalMemory1
            },
            metadatas: metadatas,
            distances: new[] { 0.1f, 0.2f });

        collection.QueryAsync(
            Arg.Any<IReadOnlyList<ReadOnlyMemory<float>>>(),
            Arg.Any<int>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(queryResult);

        var service = new HybridSearchService(backend, embedder);

        // Act
        var results = await service.SearchAsync(
            query: "learning",
            collection: "test-collection",
            opts: new SearchOptions(TopK: 10, Wing: "technical"),
            ct: CancellationToken.None);

        // Assert
        results.Should().AllSatisfy(r =>
            r.Metadata?.GetValueOrDefault("wing")?.ToString().Should().Be("technical"));
    }

    [Fact]
    public async Task SearchAsync_AppliesMinScoreThreshold()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var embedder = Substitute.For<IEmbedder>();
        var collection = Substitute.For<ICollection>();

        embedder.ModelIdentity.Returns("test-model");
        embedder.Dimensions.Returns(128);
        embedder.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new ReadOnlyMemory<float>(new float[128]) });

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>()
        ).Returns(collection);

        var queryResult = SearchTestData.CreateMockQueryResult(
            ids: new[] { "strong", "medium", "weak" },
            documents: new[]
            {
                SearchTestData.TechnicalMemory1,
                "some related content",
                "unrelated content here"
            },
            distances: new[] { 0.05f, 0.30f, 0.95f });

        collection.QueryAsync(
            Arg.Any<IReadOnlyList<ReadOnlyMemory<float>>>(),
            Arg.Any<int>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(queryResult);

        var service = new HybridSearchService(backend, embedder);

        // Act
        var results = await service.SearchAsync(
            query: "test query",
            collection: "test-collection",
            opts: new SearchOptions(TopK: 10, MinScore: 0.5f),
            ct: CancellationToken.None);

        // Assert
        results.Should().AllSatisfy(r => r.Score.Should().BeGreaterThanOrEqualTo(0.5f));
    }

    [Fact]
    public async Task SearchAsync_RespectsTopKLimit()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var embedder = Substitute.For<IEmbedder>();
        var collection = Substitute.For<ICollection>();

        embedder.ModelIdentity.Returns("test-model");
        embedder.Dimensions.Returns(128);
        embedder.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new ReadOnlyMemory<float>(new float[128]) });

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>()
        ).Returns(collection);

        var ids = Enumerable.Range(1, 20).Select(i => $"doc-{i}").ToList();
        var documents = SearchTestData.AllMemories.Concat(SearchTestData.AllMemories.Take(1)).ToList();
        var distances = Enumerable.Range(0, 20).Select(i => (float)i * 0.05f).ToList();

        var queryResult = SearchTestData.CreateMockQueryResult(
            ids: ids,
            documents: documents.Take(20).ToList(),
            distances: distances);

        collection.QueryAsync(
            Arg.Any<IReadOnlyList<ReadOnlyMemory<float>>>(),
            Arg.Any<int>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(queryResult);

        var service = new HybridSearchService(backend, embedder);

        // Act
        var results = await service.SearchAsync(
            query: "test",
            collection: "test-collection",
            opts: new SearchOptions(TopK: 5),
            ct: CancellationToken.None);

        // Assert
        results.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyResults_ReturnsEmpty()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var embedder = Substitute.For<IEmbedder>();
        var collection = Substitute.For<ICollection>();

        embedder.ModelIdentity.Returns("test-model");
        embedder.Dimensions.Returns(128);
        embedder.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new ReadOnlyMemory<float>(new float[128]) });

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>()
        ).Returns(collection);

        var queryResult = SearchTestData.CreateMockQueryResult(
            ids: Array.Empty<string>(),
            documents: Array.Empty<string>());

        collection.QueryAsync(
            Arg.Any<IReadOnlyList<ReadOnlyMemory<float>>>(),
            Arg.Any<int>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(queryResult);

        var service = new HybridSearchService(backend, embedder);

        // Act
        var results = await service.SearchAsync(
            query: "test",
            collection: "test-collection",
            opts: new SearchOptions(TopK: 10),
            ct: CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_WithCollectionNotFound_ReturnsEmpty()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var embedder = Substitute.For<IEmbedder>();

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>()
        ).Returns<ValueTask<ICollection>>(x => 
            new ValueTask<ICollection>(
                Task.FromException<ICollection>(
                    new InvalidOperationException("Collection not found"))));

        var service = new HybridSearchService(backend, embedder);

        // Act
        var results = await service.SearchAsync(
            query: "test",
            collection: "nonexistent",
            opts: new SearchOptions(TopK: 10),
            ct: CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_IncludesMetadataWithSources()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var embedder = Substitute.For<IEmbedder>();
        var collection = Substitute.For<ICollection>();

        embedder.ModelIdentity.Returns("test-model");
        embedder.Dimensions.Returns(128);
        embedder.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new ReadOnlyMemory<float>(new float[128]) });

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>()
        ).Returns(collection);

        var metadatas = new[]
        {
            (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?> { { "custom", "value" } }
        };

        var queryResult = SearchTestData.CreateMockQueryResult(
            ids: new[] { "doc-with-meta" },
            documents: new[] { "test content" },
            metadatas: metadatas,
            distances: new[] { 0.1f });

        collection.QueryAsync(
            Arg.Any<IReadOnlyList<ReadOnlyMemory<float>>>(),
            Arg.Any<int>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(queryResult);

        var service = new HybridSearchService(backend, embedder);

        // Act
        var results = await service.SearchAsync(
            query: "test",
            collection: "test-collection",
            opts: new SearchOptions(TopK: 10),
            ct: CancellationToken.None);

        // Assert
        results.Should().NotBeEmpty();
        results[0].Metadata.Should().NotBeNull();
        results[0].Metadata?.Should().ContainKey("sources");
        results[0].Metadata?.Should().ContainKey("custom");
    }
}
