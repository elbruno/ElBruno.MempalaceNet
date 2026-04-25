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

        await using var stream = File.OpenRead(path);
        var firstToken = await PeekFirstTokenAsync(stream, ct);
        stream.Seek(0, SeekOrigin.Begin);

        if (firstToken == '[')
        {
            await foreach (var item in LoadJsonArrayAsync(stream, maxItems, ct))
            {
                yield return item;
            }

            yield break;
        }

        await foreach (var item in LoadJsonLinesAsync(stream, maxItems, ct))
        {
            yield return item;
        }
    }

    private static async IAsyncEnumerable<DatasetItem> LoadJsonLinesAsync(
        Stream stream,
        int? maxItems,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var count = 0;
        using var reader = new StreamReader(stream, leaveOpen: true);

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

    private static async IAsyncEnumerable<DatasetItem> LoadJsonArrayAsync(
        Stream stream,
        int? maxItems,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var count = 0;

        await foreach (var root in JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(stream, JsonOptions, ct))
        {
            ct.ThrowIfCancellationRequested();

            if (maxItems.HasValue && count >= maxItems.Value)
                yield break;

            if (root.ValueKind != JsonValueKind.Object)
                continue;

            yield return ParseJsonArrayItem(root);
            count++;
        }
    }

    private static DatasetItem ParseJsonArrayItem(JsonElement root)
    {
        var metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["source_format"] = "json-array"
        };

        CopyScalarProperty(root, "question_type", metadata);
        CopyScalarProperty(root, "question_category", metadata);
        CopyScalarProperty(root, "question_level", metadata);

        var relevantIds = ReadStringArray(root, "answer_session_ids");
        if (relevantIds.Count == 0)
        {
            relevantIds = ReadStringArray(root, "relevant_memory_ids");
        }

        var corpusDocuments = BuildLongMemEvalCorpus(root);
        if (corpusDocuments.Count > 0)
        {
            metadata["source_format"] = "longmemeval-upstream";
            metadata["corpus_size"] = corpusDocuments.Count;
        }

        var id = GetString(root, "question_id")
            ?? GetString(root, "id")
            ?? Guid.NewGuid().ToString();

        var question = GetString(root, "question") ?? string.Empty;
        var expectedAnswer = GetString(root, "answer")
            ?? GetString(root, "expected_answer")
            ?? string.Empty;

        return new DatasetItem(id, question, expectedAnswer, relevantIds, metadata, corpusDocuments);
    }

    private static List<CorpusDocument> BuildLongMemEvalCorpus(JsonElement root)
    {
        var documents = new List<CorpusDocument>();

        if (!root.TryGetProperty("haystack_sessions", out var sessionsElem) || sessionsElem.ValueKind != JsonValueKind.Array)
            return documents;

        var sessionIds = ReadOptionalStringArray(root, "haystack_session_ids");
        var dates = ReadOptionalStringArray(root, "haystack_dates");

        var index = 0;
        foreach (var sessionElem in sessionsElem.EnumerateArray())
        {
            if (sessionElem.ValueKind != JsonValueKind.Array)
            {
                index++;
                continue;
            }

            var userTurns = new List<string>();
            foreach (var turnElem in sessionElem.EnumerateArray())
            {
                if (turnElem.ValueKind != JsonValueKind.Object)
                    continue;

                var role = GetString(turnElem, "role");
                var content = GetString(turnElem, "content");

                if (string.Equals(role, "user", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(content))
                {
                    userTurns.Add(content);
                }
            }

            if (userTurns.Count == 0)
            {
                index++;
                continue;
            }

            var sessionId = index < sessionIds.Count && !string.IsNullOrWhiteSpace(sessionIds[index])
                ? sessionIds[index]
                : $"session_{index}";

            var metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["session_id"] = sessionId,
                ["source_format"] = "longmemeval-upstream"
            };

            if (index < dates.Count && !string.IsNullOrWhiteSpace(dates[index]))
            {
                metadata["date"] = dates[index];
            }

            documents.Add(new CorpusDocument(
                sessionId ?? $"session_{index}",
                string.Join(Environment.NewLine, userTurns),
                metadata));

            index++;
        }

        return documents;
    }

    private static async ValueTask<char?> PeekFirstTokenAsync(Stream stream, CancellationToken ct)
    {
        var buffer = new byte[4096];

        while (true)
        {
            var read = await stream.ReadAsync(buffer, ct);
            if (read == 0)
                return null;

            for (var i = 0; i < read; i++)
            {
                var ch = (char)buffer[i];
                if (!char.IsWhiteSpace(ch))
                    return ch;
            }
        }
    }

    private static void CopyScalarProperty(JsonElement root, string propertyName, IDictionary<string, object?> destination)
    {
        if (!root.TryGetProperty(propertyName, out var value))
            return;

        destination[propertyName] = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.TryGetInt64(out var intValue) ? intValue : value.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private static List<string> ReadStringArray(JsonElement root, string propertyName)
    {
        var values = ReadOptionalStringArray(root, propertyName);
        return values
            .OfType<string>()
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }

    private static List<string?> ReadOptionalStringArray(JsonElement root, string propertyName)
    {
        var values = new List<string?>();
        if (!root.TryGetProperty(propertyName, out var elem) || elem.ValueKind != JsonValueKind.Array)
            return values;

        foreach (var item in elem.EnumerateArray())
        {
            values.Add(item.GetString());
        }

        return values;
    }

    private static string? GetString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var elem) && elem.ValueKind == JsonValueKind.String
            ? elem.GetString()
            : null;
    }
}
