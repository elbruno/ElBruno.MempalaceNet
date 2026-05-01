namespace MemPalace.Core.Errors;

/// <summary>
/// Exception thrown when an embedder operation fails.
/// Wraps underlying errors from embedding providers (API errors, network failures, etc.).
/// </summary>
public class EmbedderError : Exception
{
    public EmbedderError(string message) : base(message)
    {
    }

    public EmbedderError(string message, Exception innerException) : base(message, innerException)
    {
    }
}
