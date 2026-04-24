using FluentAssertions;
using MemPalace.Search;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using MemPalace.Ai.Rerank;
using NSubstitute;

namespace MemPalace.Tests.Search;

public sealed class VectorSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_ReturnsTopKResults()
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
            Documents: new[] { new[] { "doc1", "doc2", "doc3" } },
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

        var searchService = new VectorSearchService(backend, embedder);
        var options = new SearchOptions(TopK: 3);

        // Act
        var results = await searchService.SearchAsync("test query", "test-collection", options);

        // Assert
        results.Should().HaveCount(3);
        results[0].Score.Should().BeApproximately(0.9f, 0.01f); // 1 - 0.1
        results[1].Score.Should().BeApproximately(0.8f, 0.01f); // 1 - 0.2
        results[2].Score.Should().BeApproximately(0.7f, 0.01f); // 1 - 0.3
    }

    [Fact]
    public async Task SearchAsync_WithWing_PassesWhereClause()
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

        var searchService = new VectorSearchService(backend, embedder);
        var options = new SearchOptions(Wing: "conversations");

        // Act
        await searchService.SearchAsync("test", "test-collection", options);

        // Assert
        await collection.Received(1).QueryAsync(
            Arg.Any<IReadOnlyList<ReadOnlyMemory<float>>>(),
            Arg.Any<int>(),
            Arg.Is<WhereClause?>(w => w != null && w.GetType() == typeof(Eq)),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task SearchAsync_WithMinScore_FiltersResults()
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
            Documents: new[] { new[] { "doc1", "doc2", "doc3" } },
            Metadatas: new[] { new[] {
                new Dictionary<string, object?>(),
                new Dictionary<string, object?>(),
                new Dictionary<string, object?>()
            } },
            Distances: new[] { new[] { 0.1f, 0.5f, 0.9f } }
        );

        collection.QueryAsync(
            Arg.Any<IReadOnlyList<ReadOnlyMemory<float>>>(),
            Arg.Any<int>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(queryResult);

        var searchService = new VectorSearchService(backend, embedder);
        var options = new SearchOptions(MinScore: 0.6f);

        // Act
        var results = await searchService.SearchAsync("test", "test-collection", options);

        // Assert
        results.Should().HaveCount(1);
        results[0].Id.Should().Be("id1");
    }

    [Fact]
    public async Task SearchAsync_WithReranker_ReranksResults()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var embedder = Substitute.For<IEmbedder>();
        var reranker = Substitute.For<IReranker>();
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
                new Dictionary<string, object?>(),
                new Dictionary<string, object?>()
            } },
            Distances: new[] { new[] { 0.1f, 0.2f } }
        );

        collection.QueryAsync(
            Arg.Any<IReadOnlyList<ReadOnlyMemory<float>>>(),
            Arg.Any<int>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(queryResult);

        // Simulate reranker reversing the order
        reranker.RerankAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<RankedHit>>(),
            Arg.Any<CancellationToken>()
        ).Returns(call =>
        {
            var hits = call.Arg<IReadOnlyList<RankedHit>>();
            return new[] { hits[1], hits[0] };
        });

        var searchService = new VectorSearchService(backend, embedder, reranker);
        var options = new SearchOptions(Rerank: true);

        // Act
        var results = await searchService.SearchAsync("test", "test-collection", options);

        // Assert
        results[0].Id.Should().Be("id2");
        results[1].Id.Should().Be("id1");
        await reranker.Received(1).RerankAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<RankedHit>>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task SearchAsync_CollectionNotFound_ReturnsEmpty()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var embedder = Substitute.For<IEmbedder>();

        embedder.ModelIdentity.Returns("test-model");
        embedder.Dimensions.Returns(128);

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>()
        ).Returns<ICollection>(_ => throw new Exception("Collection not found"));

        var searchService = new VectorSearchService(backend, embedder);
        var options = new SearchOptions();

        // Act
        var results = await searchService.SearchAsync("test", "nonexistent", options);

        // Assert
        results.Should().BeEmpty();
    }
}
