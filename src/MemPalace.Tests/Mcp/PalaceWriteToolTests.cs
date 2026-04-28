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
/// Tests for write operations (store, update, delete) in MCP tools.
/// </summary>
public class PalaceWriteToolTests
{
    private readonly ISearchService _searchService;
    private readonly IBackend _backend;
    private readonly IKnowledgeGraph _knowledgeGraph;
    private readonly IMemorySummarizer _memorySummarizer;
    private readonly IEmbedder _embedder;
    private readonly ICollection _collection;
    private readonly MemPalaceMcpTools _tools;

    public PalaceWriteToolTests()
    {
        _searchService = Substitute.For<ISearchService>();
        _backend = Substitute.For<IBackend>();
        _knowledgeGraph = Substitute.For<IKnowledgeGraph>();
        _memorySummarizer = Substitute.For<IMemorySummarizer>();
        _embedder = Substitute.For<IEmbedder>();
        _collection = Substitute.For<ICollection>();

        // Setup default embedder behavior
        _embedder.ModelIdentity.Returns("test-embedder");
        _embedder.Dimensions.Returns(384);
        _embedder.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var texts = callInfo.Arg<IReadOnlyList<string>>();
                var embeddings = texts.Select(_ => new ReadOnlyMemory<float>(new float[384])).ToList();
                return ValueTask.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(embeddings);
            });

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
    public async Task PalaceStoreMemory_HappyPath_ReturnsMemoryId()
    {
        // Act
        var result = await _tools.PalaceStoreMemory(
            content: "Test memory content",
            collection: "test-wing",
            palace: "default");

        // Assert
        result.Should().NotBeNull();
        result.MemoryId.Should().NotBeNullOrEmpty();
        result.StoredAt.Should().NotBeNullOrEmpty();

        // Verify backend was called
        await _backend.Received(1).GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            "test-wing",
            true, // create = true
            _embedder,
            Arg.Any<CancellationToken>());

        // Verify embedder was called
        await _embedder.Received(1).EmbedAsync(
            Arg.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "Test memory content"),
            Arg.Any<CancellationToken>());

        // Verify collection.AddAsync was called
        await _collection.Received(1).AddAsync(
            Arg.Is<IReadOnlyList<EmbeddedRecord>>(records => 
                records.Count == 1 && 
                records[0].Document == "Test memory content"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PalaceStoreMemory_WithMetadata_StoresMetadata()
    {
        // Arrange
        var metadata = """{"source":"test","priority":"high"}""";

        // Act
        var result = await _tools.PalaceStoreMemory(
            content: "Test memory with metadata",
            collection: "test-wing",
            metadata: metadata);

        // Assert
        result.Should().NotBeNull();

        // Verify metadata was included
        await _collection.Received(1).AddAsync(
            Arg.Is<IReadOnlyList<EmbeddedRecord>>(records =>
                records.Count == 1 &&
                records[0].Metadata.ContainsKey("source") &&
                records[0].Metadata.ContainsKey("priority") &&
                records[0].Metadata.ContainsKey("stored_at")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PalaceStoreMemory_InvalidMetadataJson_ThrowsException()
    {
        // Arrange
        var invalidMetadata = "{invalid json}";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _tools.PalaceStoreMemory(
                content: "Test memory",
                collection: "test-wing",
                metadata: invalidMetadata));
    }

    [Fact]
    public async Task PalaceUpdateMemory_HappyPath_UpdatesContent()
    {
        // Arrange
        var existingId = "existing-123";
        _collection.GetAsync(
            Arg.Is<IReadOnlyList<string>>(ids => ids.Contains(existingId)),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>())
            .Returns(new GetResult(
                Ids: new[] { existingId },
                Documents: new[] { "Old content" },
                Metadatas: new IReadOnlyDictionary<string, object?>[] 
                { 
                    new Dictionary<string, object?> { ["key"] = "value" } 
                },
                Embeddings: new[] { new ReadOnlyMemory<float>(new float[384]) }));

        // Act
        var result = await _tools.PalaceUpdateMemory(
            id: existingId,
            collection: "test-wing",
            content: "New content");

        // Assert
        result.Should().NotBeNull();
        result.MemoryId.Should().Be(existingId);
        result.UpdatedAt.Should().NotBeNullOrEmpty();

        // Verify collection.UpsertAsync was called with new content
        await _collection.Received(1).UpsertAsync(
            Arg.Is<IReadOnlyList<EmbeddedRecord>>(records =>
                records.Count == 1 &&
                records[0].Id == existingId &&
                records[0].Document == "New content"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PalaceUpdateMemory_MemoryNotFound_ThrowsException()
    {
        // Arrange
        _collection.GetAsync(
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>())
            .Returns(new GetResult(
                Ids: Array.Empty<string>(),
                Documents: Array.Empty<string>(),
                Metadatas: Array.Empty<IReadOnlyDictionary<string, object?>>(),
                Embeddings: Array.Empty<ReadOnlyMemory<float>>()));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _tools.PalaceUpdateMemory(
                id: "non-existent",
                collection: "test-wing",
                content: "New content"));
    }

    [Fact]
    public async Task PalaceUpdateMemory_OnlyMetadata_PreservesContent()
    {
        // Arrange
        var existingId = "existing-456";
        _collection.GetAsync(
            Arg.Is<IReadOnlyList<string>>(ids => ids.Contains(existingId)),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>())
            .Returns(new GetResult(
                Ids: new[] { existingId },
                Documents: new[] { "Original content" },
                Metadatas: new IReadOnlyDictionary<string, object?>[] 
                { 
                    new Dictionary<string, object?> { ["old"] = "meta" } 
                },
                Embeddings: new[] { new ReadOnlyMemory<float>(new float[384]) }));

        // Act
        var result = await _tools.PalaceUpdateMemory(
            id: existingId,
            collection: "test-wing",
            metadata: """{"new":"meta"}""");

        // Assert
        // Verify content was preserved
        await _collection.Received(1).UpsertAsync(
            Arg.Is<IReadOnlyList<EmbeddedRecord>>(records =>
                records.Count == 1 &&
                records[0].Document == "Original content"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PalaceDeleteMemory_HappyPath_DeletesMemory()
    {
        // Arrange
        var memoryId = "to-delete-789";

        // Act
        var result = await _tools.PalaceDeleteMemory(
            id: memoryId,
            collection: "test-wing");

        // Assert
        result.Should().NotBeNull();
        result.Deleted.Should().BeTrue();
        result.MemoryId.Should().Be(memoryId);

        // Verify DeleteAsync was called
        await _collection.Received(1).DeleteAsync(
            Arg.Is<IReadOnlyList<string>>(ids => ids.Count == 1 && ids[0] == memoryId),
            Arg.Any<WhereClause?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StoreUpdateDeleteCycle_WorksEndToEnd()
    {
        // Arrange
        string? capturedId = null;

        _collection.AddAsync(Arg.Any<IReadOnlyList<EmbeddedRecord>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var records = callInfo.Arg<IReadOnlyList<EmbeddedRecord>>();
                capturedId = records[0].Id;
                return ValueTask.CompletedTask;
            });

        // Act 1: Store
        var storeResult = await _tools.PalaceStoreMemory(
            content: "Initial content",
            collection: "test-wing");

        capturedId.Should().NotBeNullOrEmpty();

        // Setup for update
        _collection.GetAsync(
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>())
            .Returns(new GetResult(
                Ids: new[] { capturedId! },
                Documents: new[] { "Initial content" },
                Metadatas: new IReadOnlyDictionary<string, object?>[] 
                { 
                    new Dictionary<string, object?>() 
                },
                Embeddings: new[] { new ReadOnlyMemory<float>(new float[384]) }));

        // Act 2: Update
        var updateResult = await _tools.PalaceUpdateMemory(
            id: capturedId!,
            collection: "test-wing",
            content: "Updated content");

        updateResult.MemoryId.Should().Be(capturedId);

        // Act 3: Delete
        var deleteResult = await _tools.PalaceDeleteMemory(
            id: capturedId!,
            collection: "test-wing");

        deleteResult.Deleted.Should().BeTrue();
        deleteResult.MemoryId.Should().Be(capturedId);
    }
}
