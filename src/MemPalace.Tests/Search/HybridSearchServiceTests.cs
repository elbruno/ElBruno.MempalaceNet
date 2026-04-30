using FluentAssertions;
using MemPalace.Search;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using NSubstitute;

namespace MemPalace.Tests.Search;

public sealed class HybridSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_CombinesVectorAndKeywordScores()
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

        var queryResult = new QueryResult(
            Ids: new[] { new[] { "id1", "id2", "id3" } },
            Documents: new[] { new[] {
                "machine learning algorithms",
                "deep learning models",
                "neural network architectures"
            } },
            Metadatas: new[] { new[] {
                new Dictionary<string, object?>(),
                new Dictionary<string, object?>(),
                new Dictionary<string, object?>()
            } },
            Distances: new[] { new[] { 0.1f, 0.2f, 0.3f } }
        );

        collection.QueryAsync(
            Arg.Any<IReadOnlyList<ReadOnlyMemory<float>>>(),
            Arg.Any<int>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(queryResult);

        var getResult = new GetResult(
            Ids: new[] { "id1", "id2", "id3" },
            Documents: new[] {
                "machine learning algorithms",
                "deep learning models",
                "neural network architectures"
            },
            Metadatas: new[] {
                (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>(),
                (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>(),
                (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>()
            }
        );

        collection.GetAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(getResult);

        var searchService = new HybridSearchService(backend, embedder);
        var options = new SearchOptions(TopK: 3);

        // Act
        var results = await searchService.SearchAsync("machine learning", "test-collection", options);

        // Assert
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.Metadata.Should().ContainKey("sources"));
        results[0].Metadata!["sources"].Should().BeEquivalentTo(new[] { "vector", "bm25" });
    }

    [Fact]
    public async Task SearchAsync_RespectsTopK()
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

        var docs = Enumerable.Range(1, 20).Select(i => $"document {i}").ToArray();
        var ids = Enumerable.Range(1, 20).Select(i => $"id{i}").ToArray();
        var metas = Enumerable.Range(1, 20).Select(_ => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>()).ToArray();
        var distances = Enumerable.Range(1, 20).Select(i => i * 0.05f).ToArray();

        var queryResult = new QueryResult(
            Ids: new[] { ids },
            Documents: new[] { docs },
            Metadatas: new[] { metas },
            Distances: new[] { distances }
        );

        collection.QueryAsync(
            Arg.Any<IReadOnlyList<ReadOnlyMemory<float>>>(),
            Arg.Any<int>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(queryResult);

        var getResult = new GetResult(
            Ids: ids,
            Documents: docs,
            Metadatas: metas
        );

        collection.GetAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(getResult);

        var searchService = new HybridSearchService(backend, embedder);
        var options = new SearchOptions(TopK: 5);

        // Act
        var results = await searchService.SearchAsync("document", "test-collection", options);

        // Assert
        results.Should().HaveCount(5);
    }

    [Fact]
    public async Task SearchAsync_FiltersWithMinScore()
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

        var queryResult = new QueryResult(
            Ids: new[] { new[] { "id1", "id2" } },
            Documents: new[] { new[] { "doc1", "doc2" } },
            Metadatas: new[] { new[] {
                (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>(),
                (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>()
            } },
            Distances: new[] { new[] { 0.1f, 0.5f } }
        );

        collection.QueryAsync(
            Arg.Any<IReadOnlyList<ReadOnlyMemory<float>>>(),
            Arg.Any<int>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(queryResult);

        var getResult = new GetResult(
            Ids: new[] { "id1", "id2" },
            Documents: new[] { "doc1", "doc2" },
            Metadatas: new[] {
                (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>(),
                (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>()
            }
        );

        collection.GetAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(getResult);

        var searchService = new HybridSearchService(backend, embedder);
        var options = new SearchOptions(MinScore: 0.01f); // Lower threshold for RRF (1/(60+1) ≈ 0.0164)

        // Act
        var results = await searchService.SearchAsync("test", "test-collection", options);

        // Assert
        results.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_EmptyResults_ReturnsEmpty()
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

        var queryResult = QueryResult.Empty(1, false);
        collection.QueryAsync(
            Arg.Any<IReadOnlyList<ReadOnlyMemory<float>>>(),
            Arg.Any<int>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(queryResult);

        var searchService = new HybridSearchService(backend, embedder);
        var options = new SearchOptions();

        // Act
        var results = await searchService.SearchAsync("test", "test-collection", options);

        // Assert
        results.Should().BeEmpty();
    }
}
