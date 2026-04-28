using System.Text.Json;

namespace MemPalace.Mcp.Security;

/// <summary>
/// File-based audit logger that writes to ~/.palace/audit.log
/// </summary>
public class FileAuditLogger : IAuditLogger
{
    private readonly string _logPath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public FileAuditLogger()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var palaceDir = Path.Combine(homeDir, ".palace");
        Directory.CreateDirectory(palaceDir);
        _logPath = Path.Combine(palaceDir, "audit.log");
    }

    public async Task LogAsync(AuditEntry entry, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            var json = JsonSerializer.Serialize(entry);
            await File.AppendAllTextAsync(_logPath, json + Environment.NewLine, ct);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
