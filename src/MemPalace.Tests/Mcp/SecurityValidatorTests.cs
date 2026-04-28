using MemPalace.Mcp.Security;
using Moq;
using Xunit;

namespace MemPalace.Tests.Mcp;

public class SecurityValidatorTests
{
    private readonly Mock<IAuditLogger> _mockAuditLogger;
    private readonly SecurityValidator _validator;

    public SecurityValidatorTests()
    {
        _mockAuditLogger = new Mock<IAuditLogger>();
        _validator = new SecurityValidator(_mockAuditLogger.Object);
    }

    [Theory]
    [InlineData("valid-collection")]
    [InlineData("test_collection")]
    [InlineData("collection.name")]
    [InlineData("collection123")]
    public void ValidateCollectionName_ValidNames_DoesNotThrow(string collectionName)
    {
        // Act & Assert (should not throw)
        _validator.ValidateCollectionName(collectionName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ValidateCollectionName_EmptyNames_ThrowsSecurityException(string? collectionName)
    {
        // Act & Assert
        Assert.Throws<SecurityException>(() => _validator.ValidateCollectionName(collectionName!));
    }

    [Theory]
    [InlineData("collection; DROP TABLE")]
    [InlineData("collection' OR '1'='1")]
    [InlineData("collection<script>")]
    [InlineData("collection@special")]
    public void ValidateCollectionName_InvalidCharacters_ThrowsSecurityException(string collectionName)
    {
        // Act & Assert
        var exception = Assert.Throws<SecurityException>(() => _validator.ValidateCollectionName(collectionName));
        Assert.Contains("invalid characters", exception.Message);
    }

    [Fact]
    public void ValidateCollectionName_TooLong_ThrowsSecurityException()
    {
        // Arrange
        var longName = new string('a', 256);

        // Act & Assert
        var exception = Assert.Throws<SecurityException>(() => _validator.ValidateCollectionName(longName));
        Assert.Contains("cannot exceed 255 characters", exception.Message);
    }

    [Fact]
    public void ValidateMemoryId_ValidId_DoesNotThrow()
    {
        // Act & Assert (should not throw)
        _validator.ValidateMemoryId("valid-id-123");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ValidateMemoryId_EmptyId_ThrowsSecurityException(string? memoryId)
    {
        // Act & Assert
        Assert.Throws<SecurityException>(() => _validator.ValidateMemoryId(memoryId!));
    }

    [Fact]
    public void ValidateMemoryId_TooLong_ThrowsSecurityException()
    {
        // Arrange
        var longId = new string('a', 513);

        // Act & Assert
        var exception = Assert.Throws<SecurityException>(() => _validator.ValidateMemoryId(longId));
        Assert.Contains("cannot exceed 512 characters", exception.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void ValidateBatchSize_ValidSizes_DoesNotThrow(int batchSize)
    {
        // Act & Assert (should not throw)
        _validator.ValidateBatchSize(batchSize);
    }

    [Fact]
    public void ValidateBatchSize_Zero_ThrowsSecurityException()
    {
        // Act & Assert
        var exception = Assert.Throws<SecurityException>(() => _validator.ValidateBatchSize(0));
        Assert.Contains("must be greater than 0", exception.Message);
    }

    [Fact]
    public void ValidateBatchSize_ExceedsMax_ThrowsSecurityException()
    {
        // Act & Assert
        var exception = Assert.Throws<SecurityException>(() => _validator.ValidateBatchSize(101));
        Assert.Contains("cannot exceed 100 items", exception.Message);
    }

    [Theory]
    [InlineData("person:alice")]
    [InlineData("project:mempalace")]
    [InlineData("agent:roy")]
    public void ValidateEntityRef_ValidRefs_DoesNotThrow(string entityRef)
    {
        // Act & Assert (should not throw)
        _validator.ValidateEntityRef(entityRef);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ValidateEntityRef_EmptyRef_ThrowsSecurityException(string? entityRef)
    {
        // Act & Assert
        Assert.Throws<SecurityException>(() => _validator.ValidateEntityRef(entityRef!));
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("no-colon-here")]
    public void ValidateEntityRef_NoColon_ThrowsSecurityException(string entityRef)
    {
        // Act & Assert
        var exception = Assert.Throws<SecurityException>(() => _validator.ValidateEntityRef(entityRef));
        Assert.Contains("must be in format 'type:id'", exception.Message);
    }

    [Theory]
    [InlineData(":empty-type")]
    [InlineData("empty-id:")]
    [InlineData(":")]
    public void ValidateEntityRef_EmptyParts_ThrowsSecurityException(string entityRef)
    {
        // Act & Assert
        var exception = Assert.Throws<SecurityException>(() => _validator.ValidateEntityRef(entityRef));
        Assert.Contains("has empty type or id", exception.Message);
    }

    [Fact]
    public async Task AuditWriteOperationAsync_LogsToAuditLogger()
    {
        // Arrange
        var operation = "palace_store";
        var collection = "test-collection";
        var memoryId = "test-id";
        var metadata = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        await _validator.AuditWriteOperationAsync(operation, collection, memoryId, metadata);

        // Assert
        _mockAuditLogger.Verify(
            logger => logger.LogAsync(
                It.Is<AuditEntry>(entry =>
                    entry.Operation == operation &&
                    entry.Collection == collection &&
                    entry.MemoryId == memoryId &&
                    entry.Metadata == metadata),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AuditWriteOperationAsync_MinimalData_LogsSuccessfully()
    {
        // Arrange
        var operation = "palace_delete";
        var collection = "test-collection";

        // Act
        await _validator.AuditWriteOperationAsync(operation, collection);

        // Assert
        _mockAuditLogger.Verify(
            logger => logger.LogAsync(
                It.Is<AuditEntry>(entry =>
                    entry.Operation == operation &&
                    entry.Collection == collection &&
                    entry.MemoryId == null &&
                    entry.Metadata == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
