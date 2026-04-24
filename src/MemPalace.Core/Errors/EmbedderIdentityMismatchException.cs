namespace MemPalace.Core.Errors;

/// <summary>
/// Thrown when an embedder's identity doesn't match what the collection expects.
/// </summary>
public sealed class EmbedderIdentityMismatchException : BackendException
{
    public EmbedderIdentityMismatchException() { }
    public EmbedderIdentityMismatchException(string message) : base(message) { }
    public EmbedderIdentityMismatchException(string message, Exception inner) : base(message, inner) { }
}
