using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using MemPalace.KnowledgeGraph;
using MemPalace.Mcp;
using MemPalace.Search;
using MemPalace.Ai.Summarization;
using NSubstitute;
using Xunit;
using FluentAssertions;

namespace MemPalace.Tests.Mcp;

/// <summary>
/// Tests for control operations (wakeup, stats) in MCP tools.
/// </summary>
public class PalaceControlOperationToolTests
{
    private readonly ISearchService _searchService;
    private readonly IBackend _backend;
    private readonly IKnowledgeGraph _knowledgeGraph;
    private readonly IMemorySummarizer _memorySummarizer;
    private readonly IEmbedder _embedder;
    private readonly ICollection _collection;
    private readonly MemPalaceMcpTools _tools;

    public PalaceControlOperationToolTests()
    {
        _searchService = Substitute.For<ISearchService>();
        _backend = Substitute.For<IBackend>();
        _knowledgeGraph = Substitute.For<IKnowledgeGraph>();
        _memorySummarizer = Substitute.For<IMemorySummarizer>();
        _embedder = Substitute.For<IEmbedder>();
        _collection = Substitute.For<ICollection>();

        // Setup default embedder behavior
        _embedder.ModelIdentity.Returns("test-embedder-v1");
        _embedder.Dimensions.Returns(384);

        // Setup backend to return mock collection
        _backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>())
            .Returns(_collection);

        _tools = new MemPalaceMcpTools(_searchService, _backend, _knowledgeGraph, _memorySummarizer, _embedder);
    }

    [Fact]
    public async Task PalaceWakeUp_WithLlmSummary_ReturnsSummary()
    {
        // Arrange
        _collection.GetAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>())
            .Returns(new GetResult(
                Ids: new[] { "id1", "id2", "id3" },
                Documents: new[] { "Memory 1", "Memory 2", "Memory 3" },
                Metadatas: new IReadOnlyDictionary<string, object?>[]
                {
                    new Dictionary<string, object?>(),
                    new Dictionary<string, object?>(),
                    new Dictionary<string, object?>()
                },
                Embeddings: null));

        _memorySummarizer.SummarizeAsync(Arg.Any<GetResult>(), Arg.Any<CancellationToken>())
            .Returns("Here's a summary of your recent memories: You've been working on three tasks...");

        // Act
        var result = await _tools.PalaceWakeUp(
            collection: "test-wing",
            days: 7,
            limit: 20);

        // Assert
        result.Should().NotBeNull();
        result.Summary.Should().Contain("summary of your recent memories");
        result.MemoriesProcessed.Should().Be(3);
        result.UsedLlm.Should().BeTrue();

        // Verify summarizer was called
        await _memorySummarizer.Received(1).SummarizeAsync(
            Arg.Any<GetResult>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PalaceWakeUp_LlmUnavailable_ReturnsFallbackList()
    {
        // Arrange
        _collection.GetAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>())
            .Returns(new GetResult(
                Ids: new[] { "id1", "id2", "id3" },
                Documents: new[] { "Memory 1 with some content", "Memory 2 with different content", "Memory 3 with more content" },
                Metadatas: new IReadOnlyDictionary<string, object?>[]
                {
                    new Dictionary<string, object?>(),
                    new Dictionary<string, object?>(),
                    new Dictionary<string, object?>()
                },
                Embeddings: null));

        _memorySummarizer.SummarizeAsync(Arg.Any<GetResult>(), Arg.Any<CancellationToken>())
            .Returns((string?)null); // LLM unavailable

        // Act
        var result = await _tools.PalaceWakeUp(
            collection: "test-wing",
            days: 7,
            limit: 20);

        // Assert
        result.Should().NotBeNull();
        result.Summary.Should().Contain("Retrieved");
        result.Summary.Should().Contain("recent memories");
        result.MemoriesProcessed.Should().Be(3);
        result.UsedLlm.Should().BeFalse();
    }

    [Fact]
    public async Task PalaceWakeUp_CustomDaysAndLimit_PassesParameters()
    {
        // Arrange
        _collection.GetAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>())
            .Returns(new GetResult(
                Ids: new[] { "id1" },
                Documents: new[] { "Memory 1" },
                Metadatas: new IReadOnlyDictionary<string, object?>[] { new Dictionary<string, object?>() },
                Embeddings: null));

        _memorySummarizer.SummarizeAsync(Arg.Any<GetResult>(), Arg.Any<CancellationToken>())
            .Returns("Summary");

        // Act
        await _tools.PalaceWakeUp(
            collection: "test-wing",
            days: 30,
            limit: 50);

        // Assert
        await _collection.Received(1).GetAsync(
            ids: null,
            limit: 50,
            include: IncludeFields.Documents | IncludeFields.Metadatas,
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PalaceGetStats_HappyPath_ReturnsStatistics()
    {
        // Arrange
        _backend.ListCollectionsAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<string> { "wing1", "wing2", "wing3" });

        // Setup different counts for each collection
        var collection1 = Substitute.For<ICollection>();
        var collection2 = Substitute.For<ICollection>();
        var collection3 = Substitute.For<ICollection>();

        collection1.CountAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(10L));
        collection2.CountAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(25L));
        collection3.CountAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(15L));

        _backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            "wing1",
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>())
            .Returns(collection1);

        _backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            "wing2",
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>())
            .Returns(collection2);

        _backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            "wing3",
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>())
            .Returns(collection3);

        // Act
        var result = await _tools.PalaceGetStats(palace: "default");

        // Assert
        result.Should().NotBeNull();
        result.PalaceId.Should().Be("default");
        result.MemoryCount.Should().Be(50); // 10 + 25 + 15
        result.WingCount.Should().Be(3);
        result.Embedder.Should().Be("test-embedder-v1");
        result.Backend.Should().Be("sqlite");

        result.WingStats.Should().ContainKey("wing1");
        result.WingStats.Should().ContainKey("wing2");
        result.WingStats.Should().ContainKey("wing3");
        result.WingStats["wing1"].Should().Be(10);
        result.WingStats["wing2"].Should().Be(25);
        result.WingStats["wing3"].Should().Be(15);
    }

    [Fact]
    public async Task PalaceGetStats_EmptyPalace_ReturnsZeroStats()
    {
        // Arrange
        _backend.ListCollectionsAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<string>());

        // Act
        var result = await _tools.PalaceGetStats(palace: "default");

        // Assert
        result.Should().NotBeNull();
        result.MemoryCount.Should().Be(0);
        result.WingCount.Should().Be(0);
        result.WingStats.Should().BeEmpty();
    }

    [Fact]
    public async Task PalaceGetStats_CollectionCountFails_RecordsZero()
    {
        // Arrange
        _backend.ListCollectionsAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<string> { "wing1", "wing2" });

        var collection1 = Substitute.For<ICollection>();
        collection1.CountAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(10L));

        // wing2 will throw exception
        _backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            "wing1",
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>())
            .Returns(collection1);

        _backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            "wing2",
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>())
            .Returns<ValueTask<ICollection>>(callInfo => throw new Exception("Collection error"));

        // Act
        var result = await _tools.PalaceGetStats(palace: "default");

        // Assert
        result.Should().NotBeNull();
        result.MemoryCount.Should().Be(10); // Only wing1 counted
        result.WingStats["wing1"].Should().Be(10);
        result.WingStats["wing2"].Should().Be(0); // Error recorded as 0
    }

    [Fact]
    public async Task PalaceGetStats_IncludesEmbedderIdentity()
    {
        // Arrange
        _backend.ListCollectionsAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<string>());

        _embedder.ModelIdentity.Returns("nomic-embed-text-v1.5-onnx");

        // Act
        var result = await _tools.PalaceGetStats(palace: "default");

        // Assert
        result.Embedder.Should().Be("nomic-embed-text-v1.5-onnx");
    }
}
