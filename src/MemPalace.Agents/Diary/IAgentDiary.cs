namespace MemPalace.Agents.Diary;

/// <summary>
/// Per-agent persistent memory diary.
/// </summary>
public interface IAgentDiary
{
    /// <summary>
    /// Appends an entry to the agent's diary.
    /// </summary>
    Task AppendAsync(
        string agentId,
        DiaryEntry entry,
        CancellationToken ct = default);

    /// <summary>
    /// Gets recent diary entries for an agent.
    /// </summary>
    Task<IReadOnlyList<DiaryEntry>> RecentAsync(
        string agentId,
        int take = 50,
        CancellationToken ct = default);

    /// <summary>
    /// Searches diary entries for an agent.
    /// </summary>
    Task<IReadOnlyList<DiaryEntry>> SearchAsync(
        string agentId,
        string query,
        int topK = 10,
        CancellationToken ct = default);
}
