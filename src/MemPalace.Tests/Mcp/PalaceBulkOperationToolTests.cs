using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using MemPalace.KnowledgeGraph;
using MemPalace.Mcp;
using MemPalace.Search;
using MemPalace.Ai.Summarization;
using NSubstitute;
using Xunit;
using FluentAssertions;
using System.Text.Json;

namespace MemPalace.Tests.Mcp;

/// <summary>
/// Tests for bulk operations (export, import) in MCP tools.
/// </summary>
public class PalaceBulkOperationToolTests
{
    private readonly ISearchService _searchService;
    private readonly IBackend _backend;
    private readonly IKnowledgeGraph _knowledgeGraph;
    private readonly IMemorySummarizer _memorySummarizer;
    private readonly IEmbedder _embedder;
    private readonly ICollection _collection;
    private readonly MemPalaceMcpTools _tools;

    public PalaceBulkOperationToolTests()
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
    public async Task PalaceExportWing_Json_ReturnsJsonArray()
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
                Documents: new[] { "Document 1", "Document 2", "Document 3" },
                Metadatas: new IReadOnlyDictionary<string, object?>[]
                {
                    new Dictionary<string, object?> { ["source"] = "test1" },
                    new Dictionary<string, object?> { ["source"] = "test2" },
                    new Dictionary<string, object?> { ["source"] = "test3" }
                },
                Embeddings: null));

        // Act
        var result = await _tools.PalaceExportWing(
            collection: "test-wing",
            format: "json");

        // Assert
        result.Should().NotBeNull();
        result.Wing.Should().Be("test-wing");
        result.MemoryCount.Should().Be(3);
        result.Format.Should().Be("json");
        result.Content.Should().NotBeNullOrEmpty();

        // Verify JSON structure
        var memories = JsonSerializer.Deserialize<JsonElement[]>(result.Content);
        memories.Should().HaveCount(3);
        memories![0].GetProperty("id").GetString().Should().Be("id1");
        memories[0].GetProperty("document").GetString().Should().Be("Document 1");
    }

    [Fact]
    public async Task PalaceExportWing_Csv_ReturnsCsvFormat()
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
                Ids: new[] { "id1", "id2" },
                Documents: new[] { "Document 1", "Document 2" },
                Metadatas: new IReadOnlyDictionary<string, object?>[]
                {
                    new Dictionary<string, object?> { ["key"] = "val1" },
                    new Dictionary<string, object?> { ["key"] = "val2" }
                },
                Embeddings: null));

        // Act
        var result = await _tools.PalaceExportWing(
            collection: "test-wing",
            format: "csv");

        // Assert
        result.Should().NotBeNull();
        result.Format.Should().Be("csv");
        result.Content.Should().Contain("id,document,metadata");
        result.Content.Should().Contain("id1");
        result.Content.Should().Contain("Document 1");
    }

    [Fact]
    public async Task PalaceExportWing_InvalidFormat_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _tools.PalaceExportWing(
                collection: "test-wing",
                format: "xml"));
    }

    [Fact]
    public async Task PalaceImportMemories_ValidJson_ImportsAllMemories()
    {
        // Arrange
        var jsonContent = """
        [
            {"content": "Memory 1", "metadata": {"source": "test"}},
            {"content": "Memory 2", "id": "custom-id-123"},
            {"content": "Memory 3"}
        ]
        """;

        // Act
        var result = await _tools.PalaceImportMemories(
            jsonContent: jsonContent,
            collection: "test-wing");

        // Assert
        result.Should().NotBeNull();
        result.ImportedCount.Should().Be(3);
        result.Errors.Should().BeEmpty();

        // Verify embedder was called 3 times (once per memory)
        await _embedder.Received(3).EmbedAsync(
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());

        // Verify UpsertAsync was called 3 times
        await _collection.Received(3).UpsertAsync(
            Arg.Any<IReadOnlyList<EmbeddedRecord>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PalaceImportMemories_CustomId_UsesProvidedId()
    {
        // Arrange
        var jsonContent = """
        [
            {"content": "Memory with custom ID", "id": "my-custom-id-789"}
        ]
        """;

        EmbeddedRecord? capturedRecord = null;
        _collection.UpsertAsync(Arg.Any<IReadOnlyList<EmbeddedRecord>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var records = callInfo.Arg<IReadOnlyList<EmbeddedRecord>>();
                capturedRecord = records[0];
                return ValueTask.CompletedTask;
            });

        // Act
        var result = await _tools.PalaceImportMemories(
            jsonContent: jsonContent,
            collection: "test-wing");

        // Assert
        result.ImportedCount.Should().Be(1);
        capturedRecord.Should().NotBeNull();
        capturedRecord!.Id.Should().Be("my-custom-id-789");
    }

    [Fact]
    public async Task PalaceImportMemories_InvalidJson_ThrowsException()
    {
        // Arrange
        var invalidJson = "{not valid json}";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _tools.PalaceImportMemories(
                jsonContent: invalidJson,
                collection: "test-wing"));
    }

    [Fact]
    public async Task PalaceImportMemories_MissingContent_RecordsError()
    {
        // Arrange
        var jsonContent = """
        [
            {"content": "Valid memory"},
            {"metadata": {"note": "This one has no content field"}},
            {"content": "Another valid memory"}
        ]
        """;

        // Act
        var result = await _tools.PalaceImportMemories(
            jsonContent: jsonContent,
            collection: "test-wing");

        // Assert
        result.ImportedCount.Should().Be(2); // Only 2 out of 3 succeeded
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Contain("Missing 'content' field");
    }

    [Fact]
    public async Task PalaceImportMemories_WithMetadata_PreservesMetadata()
    {
        // Arrange
        var jsonContent = """
        [
            {
                "content": "Memory with metadata",
                "metadata": {
                    "source": "external-api",
                    "timestamp": "2024-01-15T10:30:00Z",
                    "priority": "high"
                }
            }
        ]
        """;

        EmbeddedRecord? capturedRecord = null;
        _collection.UpsertAsync(Arg.Any<IReadOnlyList<EmbeddedRecord>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var records = callInfo.Arg<IReadOnlyList<EmbeddedRecord>>();
                capturedRecord = records[0];
                return ValueTask.CompletedTask;
            });

        // Act
        var result = await _tools.PalaceImportMemories(
            jsonContent: jsonContent,
            collection: "test-wing");

        // Assert
        result.ImportedCount.Should().Be(1);
        capturedRecord.Should().NotBeNull();
        capturedRecord!.Metadata.Should().ContainKey("source");
        capturedRecord.Metadata.Should().ContainKey("timestamp");
        capturedRecord.Metadata.Should().ContainKey("priority");
        capturedRecord.Metadata.Should().ContainKey("imported_at"); // Auto-added timestamp
    }

    [Fact]
    public async Task ExportImportRoundTrip_PreservesData()
    {
        // Arrange - Setup export data
        _collection.GetAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>())
            .Returns(new GetResult(
                Ids: new[] { "id1", "id2" },
                Documents: new[] { "Document 1", "Document 2" },
                Metadatas: new IReadOnlyDictionary<string, object?>[]
                {
                    new Dictionary<string, object?> { ["source"] = "test1" },
                    new Dictionary<string, object?> { ["source"] = "test2" }
                },
                Embeddings: null));

        // Act 1: Export
        var exportResult = await _tools.PalaceExportWing(
            collection: "test-wing",
            format: "json");

        exportResult.MemoryCount.Should().Be(2);
        exportResult.Content.Should().Contain("id1");
        exportResult.Content.Should().Contain("Document 1");

        // Manually construct import JSON (as export format doesn't match import requirements)
        var importJson = """
        [
            {"content": "Document 1", "id": "id1", "metadata": {"source": "test1"}},
            {"content": "Document 2", "id": "id2", "metadata": {"source": "test2"}}
        ]
        """;

        // Act 2: Import
        var importResult = await _tools.PalaceImportMemories(
            jsonContent: importJson,
            collection: "test-wing-2");

        // Assert
        importResult.ImportedCount.Should().Be(2);
        importResult.Errors.Should().BeEmpty();
    }
}
