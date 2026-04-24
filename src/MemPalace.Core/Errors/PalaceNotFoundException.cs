namespace MemPalace.Core.Errors;

/// <summary>
/// Thrown when a palace is not found and creation was not requested.
/// </summary>
public sealed class PalaceNotFoundException : BackendException
{
    public PalaceNotFoundException() { }
    public PalaceNotFoundException(string message) : base(message) { }
    public PalaceNotFoundException(string message, Exception inner) : base(message, inner) { }
}
