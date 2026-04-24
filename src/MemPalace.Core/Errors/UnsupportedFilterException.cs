namespace MemPalace.Core.Errors;

/// <summary>
/// Thrown when a backend cannot handle a specific filter clause.
/// </summary>
public sealed class UnsupportedFilterException : BackendException
{
    public UnsupportedFilterException() { }
    public UnsupportedFilterException(string message) : base(message) { }
    public UnsupportedFilterException(string message, Exception inner) : base(message, inner) { }
}
