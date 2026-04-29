using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace MemPalace.Backends.Sqlite;

/// <summary>
/// Validates vector BLOB formats for SQLite storage.
/// Ensures data integrity for IEEE 754 float array storage.
/// </summary>
public sealed class SqliteVecBlobValidator : IVectorFormatValidator
{
    /// <inheritdoc/>
    public bool IsValidBlobFormat(ReadOnlySpan<byte> blob)
    {
        // Empty BLOBs are invalid
        if (blob.IsEmpty)
        {
            return false;
        }

        // BLOB length must be divisible by sizeof(float) = 4 bytes
        // SQLite stores floats as 4-byte IEEE 754 values
        if (blob.Length % sizeof(float) != 0)
        {
            return false;
        }

        // Check for valid float values (no NaN or Infinity)
        var floatSpan = MemoryMarshal.Cast<byte, float>(blob);
        foreach (var value in floatSpan)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public bool ValidateDimensions(ReadOnlySpan<float> vector, int expectedDimensions)
    {
        if (expectedDimensions <= 0)
        {
            return false;
        }

        return vector.Length == expectedDimensions;
    }

    /// <inheritdoc/>
    public ValidationResult ValidateVector(VectorData vector)
    {
        var errors = new List<string>();

        // Validate expected dimensions parameter
        if (vector.ExpectedDimensions <= 0)
        {
            errors.Add($"Expected dimensions must be positive, got: {vector.ExpectedDimensions}");
        }

        // Validate vector data exists
        if (vector.Data.IsEmpty)
        {
            errors.Add("Vector data is empty");
            return ValidationResult.Failure(errors.ToArray());
        }

        // Validate dimensions match
        var span = vector.Data.Span;
        if (span.Length != vector.ExpectedDimensions)
        {
            errors.Add($"Dimension mismatch: expected {vector.ExpectedDimensions}, got {span.Length}");
        }

        // Validate float values
        for (int i = 0; i < span.Length; i++)
        {
            var value = span[i];
            if (float.IsNaN(value))
            {
                errors.Add($"Vector contains NaN at index {i}");
            }
            else if (float.IsInfinity(value))
            {
                errors.Add($"Vector contains Infinity at index {i}");
            }
        }

        return errors.Count > 0 
            ? ValidationResult.Failure(errors.ToArray())
            : ValidationResult.Success();
    }

    /// <summary>
    /// Validates a vector that's already been serialized to bytes.
    /// Useful for verifying BLOBs before inserting into database.
    /// </summary>
    /// <param name="blob">The serialized vector BLOB.</param>
    /// <param name="expectedDimensions">The expected number of dimensions.</param>
    /// <returns>Validation result with detailed errors if invalid.</returns>
    public ValidationResult ValidateBlob(ReadOnlySpan<byte> blob, int expectedDimensions)
    {
        var errors = new List<string>();

        if (expectedDimensions <= 0)
        {
            errors.Add($"Expected dimensions must be positive, got: {expectedDimensions}");
        }

        if (blob.IsEmpty)
        {
            errors.Add("BLOB is empty");
            return ValidationResult.Failure(errors.ToArray());
        }

        if (blob.Length % sizeof(float) != 0)
        {
            errors.Add($"BLOB length ({blob.Length} bytes) is not divisible by sizeof(float) ({sizeof(float)} bytes)");
        }

        var expectedBytes = expectedDimensions * sizeof(float);
        if (blob.Length != expectedBytes)
        {
            var actualDimensions = blob.Length / sizeof(float);
            errors.Add($"BLOB size mismatch: expected {expectedBytes} bytes ({expectedDimensions} dimensions), got {blob.Length} bytes ({actualDimensions} dimensions)");
        }

        if (errors.Count > 0)
        {
            return ValidationResult.Failure(errors.ToArray());
        }

        // Check for invalid float values
        var floatSpan = MemoryMarshal.Cast<byte, float>(blob);
        for (int i = 0; i < floatSpan.Length; i++)
        {
            var value = floatSpan[i];
            if (float.IsNaN(value))
            {
                errors.Add($"BLOB contains NaN at float index {i} (byte offset {i * sizeof(float)})");
            }
            else if (float.IsInfinity(value))
            {
                errors.Add($"BLOB contains Infinity at float index {i} (byte offset {i * sizeof(float)})");
            }
        }

        return errors.Count > 0 
            ? ValidationResult.Failure(errors.ToArray())
            : ValidationResult.Success();
    }
}
