using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MemPalace.Mining;

/// <summary>
/// Mines conversation transcripts (JSONL or Markdown format).
/// </summary>
public sealed class ConversationMiner : IMiner
{
    private static readonly Regex MarkdownTurnRegex = new(
        @"^##\s+(User|Assistant|Human|AI)\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    public string Name => "conversation";

    public async IAsyncEnumerable<MinedItem> MineAsync(
        MinerContext ctx,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!File.Exists(ctx.SourcePath))
            yield break;

        var content = await File.ReadAllTextAsync(ctx.SourcePath, ct);
        
        // Detect format
        var isJsonl = ctx.SourcePath.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase) ||
                      ctx.SourcePath.EndsWith(".ndjson", StringComparison.OrdinalIgnoreCase);

        if (isJsonl)
        {
            await foreach (var item in ParseJsonlAsync(ctx.SourcePath, content, ct))
                yield return item;
        }
        else
        {
            await foreach (var item in ParseMarkdownAsync(ctx.SourcePath, content, ct))
                yield return item;
        }
    }

    private static async IAsyncEnumerable<MinedItem> ParseJsonlAsync(
        string sourcePath,
        string content,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var conversationId = Path.GetFileNameWithoutExtension(sourcePath);
        var turnIndex = 0;

        foreach (var line in lines)
        {
            ct.ThrowIfCancellationRequested();
            
            if (string.IsNullOrWhiteSpace(line))
                continue;

            MinedItem? item = null;
            JsonDocument? doc = null;
            
            try
            {
                doc = JsonDocument.Parse(line);
                var root = doc.RootElement;

                var role = root.TryGetProperty("role", out var roleElem) ? roleElem.GetString() : "unknown";
                var message = root.TryGetProperty("content", out var contentElem) ? contentElem.GetString() : 
                              root.TryGetProperty("message", out var msgElem) ? msgElem.GetString() : "";

                if (!string.IsNullOrWhiteSpace(message))
                {
                    var timestamp = root.TryGetProperty("timestamp", out var tsElem) ? tsElem.GetString() : null;

                    var metadata = new Dictionary<string, object?>
                    {
                        ["role"] = role,
                        ["turn_index"] = turnIndex,
                        ["conversation_id"] = conversationId
                    };

                    if (timestamp != null)
                        metadata["timestamp"] = timestamp;

                    item = new MinedItem(
                        Id: $"{conversationId}:turn{turnIndex}",
                        Content: message,
                        Metadata: metadata);

                    turnIndex++;
                }
            }
            catch
            {
                // Skip invalid JSON lines
            }
            finally
            {
                doc?.Dispose();
            }

            if (item != null)
                yield return item;
        }

        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<MinedItem> ParseMarkdownAsync(
        string sourcePath,
        string content,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var conversationId = Path.GetFileNameWithoutExtension(sourcePath);
        var matches = MarkdownTurnRegex.Matches(content);

        if (matches.Count == 0)
            yield break;

        for (var i = 0; i < matches.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            
            var match = matches[i];
            var role = match.Groups[1].Value.ToLowerInvariant();
            
            // Extract content between this header and the next (or end of file)
            var startIndex = match.Index + match.Length;
            var endIndex = (i + 1 < matches.Count) ? matches[i + 1].Index : content.Length;
            var turnContent = content[startIndex..endIndex].Trim();

            if (string.IsNullOrWhiteSpace(turnContent))
                continue;

            var metadata = new Dictionary<string, object?>
            {
                ["role"] = role,
                ["turn_index"] = i,
                ["conversation_id"] = conversationId
            };

            yield return new MinedItem(
                Id: $"{conversationId}:turn{i}",
                Content: turnContent,
                Metadata: metadata);
        }

        await Task.CompletedTask;
    }
}
