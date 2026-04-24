namespace MemPalace.Core.Errors;

/// <summary>
/// Base exception for all backend-related errors.
/// </summary>
public class BackendException : Exception
{
    public BackendException() { }
    public BackendException(string message) : base(message) { }
    public BackendException(string message, Exception inner) : base(message, inner) { }
}
