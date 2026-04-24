namespace MemPalace.Core.Errors;

/// <summary>
/// Thrown when embedding dimensions don't match the collection's expected dimensions.
/// </summary>
public sealed class DimensionMismatchException : BackendException
{
    public DimensionMismatchException() { }
    public DimensionMismatchException(string message) : base(message) { }
    public DimensionMismatchException(string message, Exception inner) : base(message, inner) { }
}
