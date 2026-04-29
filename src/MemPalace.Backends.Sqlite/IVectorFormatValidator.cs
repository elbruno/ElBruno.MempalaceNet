using System;

namespace MemPalace.Backends.Sqlite;

/// <summary>
/// Provides validation for vector BLOB formats used in sqlite-vec storage.
/// Ensures data consistency and prevents corrupted vectors from being stored.
/// </summary>
/// <remarks>
/// This interface is particularly useful for consumers like OpenClawNet that need
/// to validate vectors before upserting to prevent data corruption issues.
/// </remarks>
/// <example>
/// <code>
/// IVectorFormatValidator validator = new SqliteVecBlobValidator();
/// 
/// // Validate a BLOB read from storage
/// byte[] blob = GetVectorBlobFromDatabase();
/// if (validator.IsValidBlobFormat(blob))
/// {
///     // Safe to use
/// }
/// 
/// // Validate vector dimensions
/// float[] embedding = new float[384];
/// if (validator.ValidateDimensions(embedding, expectedDimensions: 384))
/// {
///     await collection.AddAsync(record);
/// }
/// 
/// // Comprehensive validation
/// var result = validator.ValidateVector(new Vector(embedding, expectedDimensions: 384));
/// if (!result.IsValid)
/// {
///     Console.WriteLine(string.Join(", ", result.Errors));
/// }
/// </code>
/// </example>
public interface IVectorFormatValidator
{
    /// <summary>
    /// Validates whether a byte array represents a properly formatted vector BLOB.
    /// </summary>
    /// <param name="blob">The byte array to validate.</param>
    /// <returns>True if the BLOB is valid; otherwise false.</returns>
    /// <remarks>
    /// For SQLite storage, vectors are stored as raw IEEE 754 float arrays.
    /// Valid BLOBs must be divisible by 4 (sizeof(float)) and non-empty.
    /// </remarks>
    bool IsValidBlobFormat(ReadOnlySpan<byte> blob);

    /// <summary>
    /// Validates that a vector's dimensions match the expected count.
    /// </summary>
    /// <param name="vector">The vector data as float values.</param>
    /// <param name="expectedDimensions">The expected number of dimensions.</param>
    /// <returns>True if dimensions match; otherwise false.</returns>
    bool ValidateDimensions(ReadOnlySpan<float> vector, int expectedDimensions);

    /// <summary>
    /// Performs comprehensive validation of a vector including format and dimensional checks.
    /// </summary>
    /// <param name="vector">The vector to validate.</param>
    /// <returns>A ValidationResult containing success status and any error messages.</returns>
    ValidationResult ValidateVector(VectorData vector);
}

/// <summary>
/// Represents the result of a vector validation operation.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// Gets whether the validation succeeded.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the collection of validation error messages.
    /// Empty if IsValid is true.
    /// </summary>
    public string[] Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with error messages.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    public static ValidationResult Failure(params string[] errors) => new() 
    { 
        IsValid = false, 
        Errors = errors 
    };
}

/// <summary>
/// Represents vector data for validation purposes.
/// </summary>
public readonly record struct VectorData(
    ReadOnlyMemory<float> Data,
    int ExpectedDimensions);
