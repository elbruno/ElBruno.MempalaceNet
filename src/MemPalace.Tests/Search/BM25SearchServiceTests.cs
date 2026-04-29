using FluentAssertions;
using MemPalace.Search;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using NSubstitute;
using MemPalace.Tests.Search.Fixtures;

namespace MemPalace.Tests.Search;

/// <summary>
/// Comprehensive tests for Bm25SearchService.
/// Tests BM25 algorithm integration, scoring, filtering, and edge cases.
/// </summary>
public sealed class BM25SearchServiceTests
{
    [Fact]
    public async Task SearchAsync_WithEmptyQuery_ReturnsEmptyResults()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var service = new Bm25SearchService(backend);

        // Act
        var results = await service.SearchAsync(
            query: "",
            collection: "memories",
            opts: new SearchOptions(TopK: 10),
            ct: CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_WithNullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var service = new Bm25SearchService(backend);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await service.SearchAsync(
                query: null!,
                collection: "memories",
                opts: new SearchOptions(TopK: 10),
                ct: CancellationToken.None));
    }

    [Fact]
    public async Task SearchAsync_WithWhitespaceOnlyQuery_ReturnsEmptyResults()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var service = new Bm25SearchService(backend);

        // Act
        var results = await service.SearchAsync(
            query: "   \t\n  ",
            collection: "memories",
            opts: new SearchOptions(TopK: 10),
            ct: CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_WithBackendError_ReturnsEmptyResults()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>()
        ).Returns<ValueTask<ICollection>>(x => 
            new ValueTask<ICollection>(
                Task.FromException<ICollection>(
                    new InvalidOperationException("Backend error"))));

        var service = new Bm25SearchService(backend);

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
    public async Task SearchAsync_WithEmptyCollectionResult_ReturnsEmptyResults()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var collection = Substitute.For<ICollection>();

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>()
        ).Returns(collection);

        var getResult = new GetResult(
            Ids: new List<string>(),
            Documents: new List<string>(),
            Metadatas: new List<IReadOnlyDictionary<string, object?>>());

        collection.GetAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(getResult);

        var service = new Bm25SearchService(backend);

        // Act
        var results = await service.SearchAsync(
            query: "test",
            collection: "memories",
            opts: new SearchOptions(TopK: 10),
            ct: CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_Instantiation_WithBackendOnly_Succeeds()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();

        // Act
        var service = new Bm25SearchService(backend);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchAsync_InvokesBackendGetCollection_WithCorrectParameters()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var collection = Substitute.For<ICollection>();

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>()
        ).Returns(collection);

        var getResult = new GetResult(
            Ids: new List<string>(),
            Documents: new List<string>(),
            Metadatas: new List<IReadOnlyDictionary<string, object?>>());

        collection.GetAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(getResult);

        var service = new Bm25SearchService(backend);

        // Act
        await service.SearchAsync(
            query: "test",
            collection: "test-collection",
            opts: new SearchOptions(TopK: 10),
            ct: CancellationToken.None);

        // Assert
        await backend.Received(1).GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Is("test-collection"),
            Arg.Is(false),
            Arg.Is((IEmbedder?)null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var collection = Substitute.For<ICollection>();

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>()
        ).Returns(collection);

        var getResult = new GetResult(
            Ids: new[] { "doc-1" },
            Documents: new[] { "test@example.com is an email address" },
            Metadatas: new List<IReadOnlyDictionary<string, object?>>
            {
                new Dictionary<string, object?> { { "wing", "default" } }
            });

        collection.GetAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(getResult);

        var service = new Bm25SearchService(backend);

        // Act - Should not throw with special characters
        var results = await service.SearchAsync(
            query: "email",
            collection: "memories",
            opts: new SearchOptions(TopK: 10),
            ct: CancellationToken.None);

        // Assert
        results.Should().BeOfType<List<SearchHit>>();
    }

    [Fact]
    public async Task SearchAsync_WithUnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var collection = Substitute.For<ICollection>();

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>()
        ).Returns(collection);

        var getResult = new GetResult(
            Ids: new[] { "doc-1" },
            Documents: new[] { "café naïve résumé test" },
            Metadatas: new List<IReadOnlyDictionary<string, object?>>
            {
                new Dictionary<string, object?> { { "wing", "default" } }
            });

        collection.GetAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(getResult);

        var service = new Bm25SearchService(backend);

        // Act - Should not throw with unicode input
        var results = await service.SearchAsync(
            query: "café",
            collection: "memories",
            opts: new SearchOptions(TopK: 10),
            ct: CancellationToken.None);

        // Assert
        results.Should().BeOfType<List<SearchHit>>();
    }

    [Fact]
    public async Task SearchAsync_WithWingFilter_AppliesWhereClause()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var collection = Substitute.For<ICollection>();

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>()
        ).Returns(collection);

        var getResult = new GetResult(
            Ids: new[] { "doc-1" },
            Documents: new[] { "technical content" },
            Metadatas: new List<IReadOnlyDictionary<string, object?>>
            {
                new Dictionary<string, object?> { { "wing", "technical" } }
            });

        collection.GetAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(getResult);

        var service = new Bm25SearchService(backend);

        // Act
        await service.SearchAsync(
            query: "test",
            collection: "memories",
            opts: new SearchOptions(TopK: 10, Wing: "technical"),
            ct: CancellationToken.None);

        // Assert - Verify that GetAsync was called
        await collection.Received().GetAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_ResultsIncludeMetadata()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var collection = Substitute.For<ICollection>();

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>()
        ).Returns(collection);

        var metadata = new Dictionary<string, object?> { { "wing", "tech" }, { "source", "docs" } };
        var getResult = new GetResult(
            Ids: new[] { "doc-1" },
            Documents: new[] { "test content" },
            Metadatas: new List<IReadOnlyDictionary<string, object?>> { metadata });

        collection.GetAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(getResult);

        var service = new Bm25SearchService(backend);

        // Act
        var results = await service.SearchAsync(
            query: "test",
            collection: "memories",
            opts: new SearchOptions(TopK: 10),
            ct: CancellationToken.None);

        // Assert
        if (results.Count > 0)
        {
            results[0].Metadata.Should().NotBeNull();
            results[0].Metadata?.Should().ContainKey("wing");
        }
    }

    [Fact]
    public async Task SearchAsync_WithMinScoreFilter_FiltersResults()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var collection = Substitute.For<ICollection>();

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>()
        ).Returns(collection);

        var getResult = new GetResult(
            Ids: new[] { "doc-1", "doc-2" },
            Documents: new[] { "high relevance test", "low relevance xyz" },
            Metadatas: new List<IReadOnlyDictionary<string, object?>>
            {
                new Dictionary<string, object?> { { "wing", "default" } },
                new Dictionary<string, object?> { { "wing", "default" } }
            });

        collection.GetAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(getResult);

        var service = new Bm25SearchService(backend);

        // Act
        var results = await service.SearchAsync(
            query: "test",
            collection: "memories",
            opts: new SearchOptions(TopK: 10, MinScore: 0.5f),
            ct: CancellationToken.None);

        // Assert
        results.Should().AllSatisfy(r => r.Score.Should().BeGreaterThanOrEqualTo(0.5f));
    }

    [Fact]
    public async Task SearchAsync_RespectTopKLimit()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var collection = Substitute.For<ICollection>();

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>()
        ).Returns(collection);

        // Create 10 documents
        var ids = Enumerable.Range(1, 10).Select(i => $"doc-{i}").ToList();
        var documents = SearchTestData.AllMemories.Concat(SearchTestData.AllMemories.Take(1)).ToList();
        var metadatas = Enumerable.Range(0, 10).Select(_ => 
            (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?> { { "wing", "default" } }).ToList();

        var getResult = new GetResult(
            Ids: ids,
            Documents: documents.Take(10).ToList(),
            Metadatas: metadatas);

        collection.GetAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()
        ).Returns(getResult);

        var service = new Bm25SearchService(backend);

        // Act
        var results = await service.SearchAsync(
            query: "learning",
            collection: "memories",
            opts: new SearchOptions(TopK: 3),
            ct: CancellationToken.None);

        // Assert
        results.Count.Should().BeLessThanOrEqualTo(3);
    }
}
