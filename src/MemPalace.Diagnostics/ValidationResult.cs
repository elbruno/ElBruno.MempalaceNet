namespace MemPalace.Diagnostics;

/// <summary>
/// Represents the result of an SLA validation check.
/// </summary>
/// <remarks>
/// This class provides detailed information about whether a set of operations
/// meet their defined SLA thresholds, along with any validation errors.
/// </remarks>
public class ValidationResult
{
    /// <summary>
    /// Gets or sets whether all SLA validations passed.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the list of validation errors.
    /// </summary>
    /// <remarks>
    /// Each error string describes which operation failed its SLA threshold
    /// and by how much.
    /// </remarks>
    public string[] Errors { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the dictionary of operation names to their pass/fail status.
    /// </summary>
    public Dictionary<string, bool> OperationResults { get; set; } = new();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A validation result with IsValid = true and no errors.</returns>
    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">Array of error messages describing validation failures.</param>
    /// <returns>A validation result with IsValid = false and the provided errors.</returns>
    public static ValidationResult Failure(params string[] errors)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = errors
        };
    }
}
