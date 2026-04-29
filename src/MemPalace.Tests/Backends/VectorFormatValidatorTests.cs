using System;
using System.Buffers.Binary;
using FluentAssertions;
using MemPalace.Backends.Sqlite;
using Xunit;

namespace MemPalace.Tests.Backends;

/// <summary>
/// Tests for SQLite vector format validation.
/// Covers valid formats, corrupted BLOBs, dimension mismatches, and edge cases.
/// </summary>
public sealed class VectorFormatValidatorTests
{
    private readonly SqliteVecBlobValidator _validator = new();

    #region IsValidBlobFormat Tests

    [Fact]
    public void IsValidBlobFormat_ValidBlob_ReturnsTrue()
    {
        // Arrange: 3-dimensional vector [1.0, 2.0, 3.0]
        var blob = new byte[12];
        BitConverter.TryWriteBytes(blob.AsSpan(0, 4), 1.0f);
        BitConverter.TryWriteBytes(blob.AsSpan(4, 4), 2.0f);
        BitConverter.TryWriteBytes(blob.AsSpan(8, 4), 3.0f);

        // Act
        var result = _validator.IsValidBlobFormat(blob);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidBlobFormat_EmptyBlob_ReturnsFalse()
    {
        // Arrange
        var blob = Array.Empty<byte>();

        // Act
        var result = _validator.IsValidBlobFormat(blob);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidBlobFormat_NonDivisibleByFour_ReturnsFalse()
    {
        // Arrange: 5 bytes (not divisible by sizeof(float) = 4)
        var blob = new byte[5];

        // Act
        var result = _validator.IsValidBlobFormat(blob);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidBlobFormat_ContainsNaN_ReturnsFalse()
    {
        // Arrange: Vector with NaN value
        var blob = new byte[12];
        BitConverter.TryWriteBytes(blob.AsSpan(0, 4), 1.0f);
        BitConverter.TryWriteBytes(blob.AsSpan(4, 4), float.NaN);
        BitConverter.TryWriteBytes(blob.AsSpan(8, 4), 3.0f);

        // Act
        var result = _validator.IsValidBlobFormat(blob);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidBlobFormat_ContainsPositiveInfinity_ReturnsFalse()
    {
        // Arrange: Vector with positive infinity
        var blob = new byte[8];
        BitConverter.TryWriteBytes(blob.AsSpan(0, 4), 1.0f);
        BitConverter.TryWriteBytes(blob.AsSpan(4, 4), float.PositiveInfinity);

        // Act
        var result = _validator.IsValidBlobFormat(blob);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidBlobFormat_ContainsNegativeInfinity_ReturnsFalse()
    {
        // Arrange: Vector with negative infinity
        var blob = new byte[8];
        BitConverter.TryWriteBytes(blob.AsSpan(0, 4), float.NegativeInfinity);
        BitConverter.TryWriteBytes(blob.AsSpan(4, 4), 2.0f);

        // Act
        var result = _validator.IsValidBlobFormat(blob);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidBlobFormat_LargeValidVector_ReturnsTrue()
    {
        // Arrange: 384-dimensional vector (typical embedding size)
        var blob = new byte[384 * 4];
        for (int i = 0; i < 384; i++)
        {
            BitConverter.TryWriteBytes(blob.AsSpan(i * 4, 4), (float)i * 0.1f);
        }

        // Act
        var result = _validator.IsValidBlobFormat(blob);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region ValidateDimensions Tests

    [Fact]
    public void ValidateDimensions_MatchingDimensions_ReturnsTrue()
    {
        // Arrange
        var vector = new float[] { 1.0f, 2.0f, 3.0f };

        // Act
        var result = _validator.ValidateDimensions(vector, expectedDimensions: 3);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateDimensions_MismatchedDimensions_ReturnsFalse()
    {
        // Arrange
        var vector = new float[] { 1.0f, 2.0f, 3.0f };

        // Act
        var result = _validator.ValidateDimensions(vector, expectedDimensions: 5);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateDimensions_ZeroExpectedDimensions_ReturnsFalse()
    {
        // Arrange
        var vector = new float[] { 1.0f, 2.0f };

        // Act
        var result = _validator.ValidateDimensions(vector, expectedDimensions: 0);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateDimensions_NegativeExpectedDimensions_ReturnsFalse()
    {
        // Arrange
        var vector = new float[] { 1.0f, 2.0f };

        // Act
        var result = _validator.ValidateDimensions(vector, expectedDimensions: -1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateDimensions_EmptyVector_MatchesZeroLength()
    {
        // Arrange
        var vector = Array.Empty<float>();

        // Act - even though expectedDimensions is 0, this should fail the positive check
        var result = _validator.ValidateDimensions(vector, expectedDimensions: 0);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ValidateVector Tests

    [Fact]
    public void ValidateVector_ValidVector_ReturnsSuccess()
    {
        // Arrange
        var vectorData = new VectorData(
            Data: new float[] { 0.1f, 0.2f, 0.3f },
            ExpectedDimensions: 3);

        // Act
        var result = _validator.ValidateVector(vectorData);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateVector_EmptyVector_ReturnsFailure()
    {
        // Arrange
        var vectorData = new VectorData(
            Data: ReadOnlyMemory<float>.Empty,
            ExpectedDimensions: 3);

        // Act
        var result = _validator.ValidateVector(vectorData);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("empty"));
    }

    [Fact]
    public void ValidateVector_DimensionMismatch_ReturnsFailureWithMessage()
    {
        // Arrange
        var vectorData = new VectorData(
            Data: new float[] { 0.1f, 0.2f },
            ExpectedDimensions: 5);

        // Act
        var result = _validator.ValidateVector(vectorData);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Dimension mismatch"));
        result.Errors.Should().Contain(e => e.Contains("expected 5") && e.Contains("got 2"));
    }

    [Fact]
    public void ValidateVector_ContainsNaN_ReturnsFailureWithIndex()
    {
        // Arrange
        var vectorData = new VectorData(
            Data: new float[] { 0.1f, float.NaN, 0.3f },
            ExpectedDimensions: 3);

        // Act
        var result = _validator.ValidateVector(vectorData);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("NaN") && e.Contains("index 1"));
    }

    [Fact]
    public void ValidateVector_ContainsInfinity_ReturnsFailureWithIndex()
    {
        // Arrange
        var vectorData = new VectorData(
            Data: new float[] { 0.1f, 0.2f, float.PositiveInfinity },
            ExpectedDimensions: 3);

        // Act
        var result = _validator.ValidateVector(vectorData);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Infinity") && e.Contains("index 2"));
    }

    [Fact]
    public void ValidateVector_NegativeExpectedDimensions_ReturnsFailure()
    {
        // Arrange
        var vectorData = new VectorData(
            Data: new float[] { 0.1f, 0.2f },
            ExpectedDimensions: -1);

        // Act
        var result = _validator.ValidateVector(vectorData);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("positive"));
    }

    [Fact]
    public void ValidateVector_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange: dimension mismatch + NaN + infinity
        var vectorData = new VectorData(
            Data: new float[] { float.NaN, float.PositiveInfinity },
            ExpectedDimensions: 5);

        // Act
        var result = _validator.ValidateVector(vectorData);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(1);
        result.Errors.Should().Contain(e => e.Contains("Dimension mismatch"));
        result.Errors.Should().Contain(e => e.Contains("NaN"));
        result.Errors.Should().Contain(e => e.Contains("Infinity"));
    }

    #endregion

    #region ValidateBlob Tests

    [Fact]
    public void ValidateBlob_ValidBlobWithMatchingDimensions_ReturnsSuccess()
    {
        // Arrange: 2-dimensional vector [1.5, 2.5]
        var blob = new byte[8];
        BitConverter.TryWriteBytes(blob.AsSpan(0, 4), 1.5f);
        BitConverter.TryWriteBytes(blob.AsSpan(4, 4), 2.5f);

        // Act
        var result = _validator.ValidateBlob(blob, expectedDimensions: 2);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateBlob_EmptyBlob_ReturnsFailure()
    {
        // Arrange
        var blob = Array.Empty<byte>();

        // Act
        var result = _validator.ValidateBlob(blob, expectedDimensions: 3);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("empty"));
    }

    [Fact]
    public void ValidateBlob_SizeMismatch_ReturnsFailureWithDetails()
    {
        // Arrange: 3 floats (12 bytes) but expecting 5 dimensions
        var blob = new byte[12];
        BitConverter.TryWriteBytes(blob.AsSpan(0, 4), 1.0f);
        BitConverter.TryWriteBytes(blob.AsSpan(4, 4), 2.0f);
        BitConverter.TryWriteBytes(blob.AsSpan(8, 4), 3.0f);

        // Act
        var result = _validator.ValidateBlob(blob, expectedDimensions: 5);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.Contains("BLOB size mismatch") && 
            e.Contains("20 bytes") && // 5 * 4
            e.Contains("12 bytes") &&
            e.Contains("3 dimensions"));
    }

    [Fact]
    public void ValidateBlob_NonDivisibleByFour_ReturnsFailure()
    {
        // Arrange: 7 bytes (not divisible by 4)
        var blob = new byte[7];

        // Act
        var result = _validator.ValidateBlob(blob, expectedDimensions: 2);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("not divisible"));
    }

    [Fact]
    public void ValidateBlob_ContainsNaN_ReturnsFailureWithByteOffset()
    {
        // Arrange: [1.0, NaN, 3.0]
        var blob = new byte[12];
        BitConverter.TryWriteBytes(blob.AsSpan(0, 4), 1.0f);
        BitConverter.TryWriteBytes(blob.AsSpan(4, 4), float.NaN);
        BitConverter.TryWriteBytes(blob.AsSpan(8, 4), 3.0f);

        // Act
        var result = _validator.ValidateBlob(blob, expectedDimensions: 3);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.Contains("NaN") && 
            e.Contains("index 1") &&
            e.Contains("byte offset 4"));
    }

    [Fact]
    public void ValidateBlob_NegativeExpectedDimensions_ReturnsFailure()
    {
        // Arrange
        var blob = new byte[8];

        // Act
        var result = _validator.ValidateBlob(blob, expectedDimensions: -1);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("positive"));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ValidateVector_VeryLargeVector_ValidatesCorrectly()
    {
        // Arrange: 1536-dimensional vector (OpenAI text-embedding-3-large size)
        var data = new float[1536];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (float)Math.Sin(i * 0.01);
        }
        var vectorData = new VectorData(data, ExpectedDimensions: 1536);

        // Act
        var result = _validator.ValidateVector(vectorData);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateBlob_SingleFloatVector_ValidatesCorrectly()
    {
        // Arrange: 1-dimensional vector
        var blob = new byte[4];
        BitConverter.TryWriteBytes(blob, 42.0f);

        // Act
        var result = _validator.ValidateBlob(blob, expectedDimensions: 1);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateVector_ZeroValues_AreValid()
    {
        // Arrange: all zeros is a valid vector
        var vectorData = new VectorData(
            Data: new float[] { 0.0f, 0.0f, 0.0f },
            ExpectedDimensions: 3);

        // Act
        var result = _validator.ValidateVector(vectorData);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateVector_NegativeValues_AreValid()
    {
        // Arrange: negative values are valid
        var vectorData = new VectorData(
            Data: new float[] { -1.0f, -2.0f, -3.0f },
            ExpectedDimensions: 3);

        // Act
        var result = _validator.ValidateVector(vectorData);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidationResult_Success_HasCorrectState()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidationResult_Failure_HasCorrectState()
    {
        // Act
        var result = ValidationResult.Failure("Error 1", "Error 2");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Error 1");
        result.Errors.Should().Contain("Error 2");
    }

    #endregion
}
