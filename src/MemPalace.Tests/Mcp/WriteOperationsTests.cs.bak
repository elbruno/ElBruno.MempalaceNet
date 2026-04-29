using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using MemPalace.KnowledgeGraph;
using MemPalace.Mcp.Security;
using MemPalace.Mcp.Tools;
using MemPalace.Ai;
using Moq;
using Xunit;

namespace MemPalace.Tests.Mcp;

public class WriteOperationsTests
{
    private readonly Mock<IBackend> _mockBackend;
    private readonly Mock<IEmbedder> _mockEmbedder;
    private readonly Mock<SecurityValidator> _mockValidator;
    private readonly Mock<IConfirmationPrompt> _mockConfirmation;
    private readonly Mock<ICollection> _mockCollection;
    private readonly WriteTools _writeTools;

    public WriteOperationsTests()
    {
        _mockBackend = new Mock<IBackend>();
        _mockEmbedder = new Mock<Core.Ai.IEmbedder>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        _mockValidator = new Mock<SecurityValidator>(mockAuditLogger.Object);
        _mockConfirmation = new Mock<IConfirmationPrompt>();
        _mockCollection = new Mock<ICollection>();

        _writeTools = new WriteTools(
            _mockBackend.Object,
            _mockEmbedder.Object,
            _mockValidator.Object,
            _mockConfirmation.Object);
    }

    [Fact]
    public async Task PalaceStore_ValidInput_StoresMemory()
    {
        // Arrange
        var content = "Test memory content";
        var collection = "test-collection";
        var embedding = new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f, 0.3f });

        _mockEmbedder.Setup(e => e.GenerateEmbeddingAsync(content, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        _mockBackend.Setup(b => b.GetCollectionAsync(
                It.IsAny<PalaceRef>(),
                collection,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockCollection.Object);

        _mockCollection.Setup(c => c.AddAsync(
                It.IsAny<IReadOnlyList<EmbeddedRecord>>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _writeTools.PalaceStore(content, collection);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
        Assert.Equal("stored", result.Status);

        _mockValidator.Verify(v => v.ValidateCollectionName(collection), Times.Once);
        _mockEmbedder.Verify(e => e.GenerateEmbeddingAsync(content, It.IsAny<CancellationToken>()), Times.Once);
        _mockCollection.Verify(c => c.AddAsync(
            It.Is<IReadOnlyList<EmbeddedRecord>>(r => r.Count == 1 && r[0].Document == content),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PalaceStore_EmptyContent_ThrowsException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _writeTools.PalaceStore("", "test-collection"));
    }

    [Fact]
    public async Task PalaceUpdate_ValidInput_UpdatesMemory()
    {
        // Arrange
        var id = "test-id";
        var newContent = "Updated content";
        var collection = "test-collection";
        var embedding = new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f, 0.3f });

        _mockEmbedder.Setup(e => e.GenerateEmbeddingAsync(newContent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        _mockBackend.Setup(b => b.GetCollectionAsync(
                It.IsAny<PalaceRef>(),
                collection,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockCollection.Object);

        var existingResult = new GetResult(
            Ids: new List<string> { id },
            Documents: new List<string> { "Old content" },
            Metadatas: new List<IReadOnlyDictionary<string, object?>> { new Dictionary<string, object?>() },
            Embeddings: null
        );

        _mockCollection.Setup(c => c.GetAsync(
                It.Is<IReadOnlyList<string>>(ids => ids.Contains(id)),
                null, null, 0,
                It.IsAny<IncludeFields>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingResult);

        _mockCollection.Setup(c => c.UpsertAsync(
                It.IsAny<IReadOnlyList<EmbeddedRecord>>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _writeTools.PalaceUpdate(id, newContent, null, collection);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("updated", result.Status);

        _mockValidator.Verify(v => v.ValidateMemoryId(id), Times.Once);
        _mockValidator.Verify(v => v.ValidateCollectionName(collection), Times.Once);
    }

    [Fact]
    public async Task PalaceDelete_WithConfirmation_DeletesMemory()
    {
        // Arrange
        var id = "test-id";
        var collection = "test-collection";

        _mockConfirmation.Setup(c => c.ConfirmAsync(
                "delete memory",
                $"{collection}/{id}",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockBackend.Setup(b => b.GetCollectionAsync(
                It.IsAny<PalaceRef>(),
                collection,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockCollection.Object);

        _mockCollection.Setup(c => c.DeleteAsync(
                It.Is<IReadOnlyList<string>>(ids => ids.Contains(id)),
                null,
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _writeTools.PalaceDelete(id, collection);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("deleted", result.Status);

        _mockConfirmation.Verify(c => c.ConfirmAsync(
            "delete memory",
            $"{collection}/{id}",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PalaceDelete_WithoutConfirmation_CancelsDelete()
    {
        // Arrange
        var id = "test-id";
        var collection = "test-collection";

        _mockConfirmation.Setup(c => c.ConfirmAsync(
                "delete memory",
                $"{collection}/{id}",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _writeTools.PalaceDelete(id, collection);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("cancelled", result.Status);

        _mockCollection.Verify(c => c.DeleteAsync(
            It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<WhereClause>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PalaceBatchStore_ValidInput_StoresMultipleMemories()
    {
        // Arrange
        var documents = new[] { "Doc 1", "Doc 2", "Doc 3" };
        var collection = "test-collection";
        var embedding = new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f, 0.3f });

        _mockEmbedder.Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        _mockBackend.Setup(b => b.GetCollectionAsync(
                It.IsAny<PalaceRef>(),
                collection,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockCollection.Object);

        _mockCollection.Setup(c => c.AddAsync(
                It.IsAny<IReadOnlyList<EmbeddedRecord>>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _writeTools.PalaceBatchStore(documents, collection);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Ids.Length);
        Assert.Equal(3, result.Count);
        Assert.Equal("stored", result.Status);

        _mockValidator.Verify(v => v.ValidateBatchSize(3), Times.Once);
        _mockEmbedder.Verify(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task PalaceBatchStore_ExceedsBatchLimit_ThrowsException()
    {
        // Arrange
        var documents = new string[101]; // Exceeds max batch size of 100
        for (int i = 0; i < 101; i++)
        {
            documents[i] = $"Doc {i}";
        }

        _mockValidator.Setup(v => v.ValidateBatchSize(101))
            .Throws(new SecurityException("Batch size cannot exceed 100 items"));

        // Act & Assert
        await Assert.ThrowsAsync<SecurityException>(
            () => _writeTools.PalaceBatchStore(documents, "test-collection"));
    }

    [Fact]
    public async Task PalaceCreateCollection_ValidName_CreatesCollection()
    {
        // Arrange
        var collection = "new-collection";

        _mockBackend.Setup(b => b.GetCollectionAsync(
                It.IsAny<PalaceRef>(),
                collection,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockCollection.Object);

        // Act
        var result = await _writeTools.PalaceCreateCollection(collection);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(collection, result.Collection);
        Assert.Equal("created", result.Status);

        _mockValidator.Verify(v => v.ValidateCollectionName(collection), Times.Once);
    }

    [Fact]
    public async Task PalaceDeleteCollection_WithConfirmation_DeletesCollection()
    {
        // Arrange
        var collection = "test-collection";

        _mockConfirmation.Setup(c => c.ConfirmAsync(
                "delete collection",
                collection,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockBackend.Setup(b => b.DeleteCollectionAsync(
                It.IsAny<PalaceRef>(),
                collection,
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _writeTools.PalaceDeleteCollection(collection);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(collection, result.Collection);
        Assert.Equal("deleted", result.Status);

        _mockConfirmation.Verify(c => c.ConfirmAsync(
            "delete collection",
            collection,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
