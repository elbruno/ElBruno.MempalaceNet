namespace MemPalace.Agents.Diary;

/// <summary>
/// Diary entry for an agent.
/// </summary>
public record DiaryEntry(
    string AgentId,
    DateTimeOffset At,
    string Role,
    string Content,
    IReadOnlyDictionary<string, object?>? Metadata = null);
