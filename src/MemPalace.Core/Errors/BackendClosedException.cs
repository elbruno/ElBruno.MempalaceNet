namespace MemPalace.Core.Errors;

/// <summary>
/// Thrown when attempting operations on a closed backend.
/// </summary>
public sealed class BackendClosedException : BackendException
{
    public BackendClosedException() : base("Backend has been closed.") { }
    public BackendClosedException(string message) : base(message) { }
    public BackendClosedException(string message, Exception inner) : base(message, inner) { }
}
