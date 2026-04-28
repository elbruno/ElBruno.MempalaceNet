namespace MemPalace.Mcp.Security;

/// <summary>
/// Interface for prompting user confirmation for destructive operations.
/// </summary>
public interface IConfirmationPrompt
{
    /// <summary>
    /// Prompts the user to confirm a destructive operation.
    /// Returns true if confirmed, false otherwise.
    /// </summary>
    Task<bool> ConfirmAsync(string operation, string target, CancellationToken ct = default);
}

/// <summary>
/// Default confirmation prompt that always returns true.
/// In a real MCP server, this would integrate with the client's confirmation UI.
/// </summary>
public class DefaultConfirmationPrompt : IConfirmationPrompt
{
    public Task<bool> ConfirmAsync(string operation, string target, CancellationToken ct = default)
    {
        // In MCP, we would send a confirmation request to the client
        // For now, we log a warning and return true (auto-confirm)
        Console.Error.WriteLine($"[WARNING] Destructive operation: {operation} on {target}");
        Console.Error.WriteLine("[INFO] Auto-confirming (in production, this would require user confirmation)");
        return Task.FromResult(true);
    }
}
