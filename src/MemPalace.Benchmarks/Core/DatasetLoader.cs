using System.Text.Json;

namespace MemPalace.Benchmarks.Core;

/// <summary>
/// Loads benchmark datasets from JSONL files.
/// </summary>
public static class DatasetLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Loads a dataset from a JSONL file.
    /// </summary>
    public static async IAsyncEnumerable<DatasetItem> LoadAsync(
        string path,
        int? maxItems = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Dataset file not found: {path}");

        var count = 0;
        await using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) != null && (maxItems == null || count < maxItems))
        {
            ct.ThrowIfCancellationRequested();
            
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var doc = JsonSerializer.Deserialize<JsonDocument>(line, JsonOptions);
            if (doc == null)
                continue;

            var root = doc.RootElement;
            
            var id = root.GetProperty("id").GetString() ?? Guid.NewGuid().ToString();
            var question = root.GetProperty("question").GetString() ?? "";
            var expectedAnswer = root.TryGetProperty("expected_answer", out var ansElem) 
                ? (ansElem.GetString() ?? "") 
                : "";
            
            var relevantIds = new List<string>();
            if (root.TryGetProperty("relevant_memory_ids", out var idsElem) && idsElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var idElem in idsElem.EnumerateArray())
                {
                    var memId = idElem.GetString();
                    if (memId != null)
                        relevantIds.Add(memId);
                }
            }

            var metadata = new Dictionary<string, object?>();
            if (root.TryGetProperty("metadata", out var metaElem) && metaElem.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in metaElem.EnumerateObject())
                {
                    metadata[prop.Name] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString(),
                        JsonValueKind.Number => prop.Value.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => null
                    };
                }
            }

            yield return new DatasetItem(id, question, expectedAnswer, relevantIds, metadata);
            count++;
        }
    }
}
