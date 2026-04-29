using MemPalace.KnowledgeGraph;
using MemPalace.Mcp.Security;
using MemPalace.Mcp.Tools;
using Moq;
using Xunit;

namespace MemPalace.Tests.Mcp;

public class KnowledgeGraphWriteToolsTests
{
    private readonly Mock<IKnowledgeGraph> _mockKnowledgeGraph;
    private readonly Mock<SecurityValidator> _mockValidator;
    private readonly KnowledgeGraphWriteTools _kgTools;

    public KnowledgeGraphWriteToolsTests()
    {
        _mockKnowledgeGraph = new Mock<IKnowledgeGraph>();
        var mockAuditLogger = new Mock<IAuditLogger>();
        _mockValidator = new Mock<SecurityValidator>(mockAuditLogger.Object);

        _kgTools = new KnowledgeGraphWriteTools(
            _mockKnowledgeGraph.Object,
            _mockValidator.Object);
    }

    [Fact]
    public async Task KgAddEntity_ValidInput_AddsEntity()
    {
        // Arrange
        var entity = "person:alice";
        var properties = new Dictionary<string, object>
        {
            ["name"] = "Alice Smith",
            ["role"] = "engineer"
        };

        _mockKnowledgeGraph.Setup(kg => kg.AddAsync(
                It.IsAny<IReadOnlyList<Triple>>(),
                It.IsAny<DateTimeOffset>(),
                null,
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _kgTools.KgAddEntity(entity, properties);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity, result.Entity);
        Assert.Equal("added", result.Status);

        _mockValidator.Verify(v => v.ValidateEntityRef(entity), Times.Once);
        _mockKnowledgeGraph.Verify(kg => kg.AddAsync(
            It.Is<IReadOnlyList<Triple>>(triples => triples.Count == 1),
            It.IsAny<DateTimeOffset>(),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task KgAddRelationship_ValidInput_AddsRelationship()
    {
        // Arrange
        var subject = "person:alice";
        var predicate = "works-on";
        var obj = "project:mempalace";

        _mockKnowledgeGraph.Setup(kg => kg.AddAsync(
                It.IsAny<IReadOnlyList<Triple>>(),
                It.IsAny<DateTimeOffset>(),
                null,
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _kgTools.KgAddRelationship(subject, predicate, obj);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(subject, result.Subject);
        Assert.Equal(predicate, result.Predicate);
        Assert.Equal(obj, result.Object);
        Assert.Equal("added", result.Status);

        _mockValidator.Verify(v => v.ValidateEntityRef(subject), Times.Once);
        _mockValidator.Verify(v => v.ValidateEntityRef(obj), Times.Once);
        _mockKnowledgeGraph.Verify(kg => kg.AddAsync(
            It.Is<IReadOnlyList<Triple>>(triples =>
                triples.Count == 1 &&
                triples[0].Predicate == predicate),
            It.IsAny<DateTimeOffset>(),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task KgAddRelationship_WithTemporalValidity_AddsWithDates()
    {
        // Arrange
        var subject = "person:alice";
        var predicate = "works-on";
        var obj = "project:mempalace";
        var validFrom = "2024-01-01T00:00:00Z";
        var validTo = "2024-12-31T23:59:59Z";

        _mockKnowledgeGraph.Setup(kg => kg.AddAsync(
                It.IsAny<IReadOnlyList<Triple>>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _kgTools.KgAddRelationship(subject, predicate, obj, validFrom, validTo);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("added", result.Status);

        _mockKnowledgeGraph.Verify(kg => kg.AddAsync(
            It.IsAny<IReadOnlyList<Triple>>(),
            It.Is<DateTimeOffset>(dto => dto == DateTimeOffset.Parse(validFrom)),
            It.Is<DateTimeOffset?>(dto => dto == DateTimeOffset.Parse(validTo)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task KgAddRelationship_EmptyPredicate_ThrowsException()
    {
        // Arrange
        var subject = "person:alice";
        var obj = "project:mempalace";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _kgTools.KgAddRelationship(subject, "", obj));
    }

    [Fact]
    public async Task KgAddRelationship_InvalidSubject_ValidatesAndThrows()
    {
        // Arrange
        var invalidSubject = "invalid-subject";
        var predicate = "works-on";
        var obj = "project:mempalace";

        _mockValidator.Setup(v => v.ValidateEntityRef(invalidSubject))
            .Throws(new SecurityException("Entity reference 'invalid-subject' must be in format 'type:id'"));

        // Act & Assert
        await Assert.ThrowsAsync<SecurityException>(
            () => _kgTools.KgAddRelationship(invalidSubject, predicate, obj));
    }

    [Fact]
    public async Task KgAddRelationship_InvalidObject_ValidatesAndThrows()
    {
        // Arrange
        var subject = "person:alice";
        var predicate = "works-on";
        var invalidObj = "invalid-object";

        _mockValidator.Setup(v => v.ValidateEntityRef(invalidObj))
            .Throws(new SecurityException("Entity reference 'invalid-object' must be in format 'type:id'"));

        // Act & Assert
        await Assert.ThrowsAsync<SecurityException>(
            () => _kgTools.KgAddRelationship(subject, predicate, invalidObj));
    }
}
