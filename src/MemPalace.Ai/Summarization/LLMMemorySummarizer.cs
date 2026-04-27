using MemPalace.Core.Backends;
using Microsoft.Extensions.AI;

namespace MemPalace.Ai.Summarization;

/// <summary>
/// LLM-based summarizer using Microsoft.Extensions.AI IChatClient.
/// Default: Local-first LLM (Qwen2.5-0.5B via ElBruno.LocalLLMs).
/// Cloud opt-in: OpenAI/Azure via environment variables or DI override.
/// </summary>
public sealed class LLMMemorySummarizer : IMemorySummarizer
{
    private readonly IChatClient _chatClient;

    private const string SystemPrompt = @"You are a memory assistant. Given a list of recent memories from a user's palace, generate a concise conversational summary highlighting:
1. Key themes or topics
2. Notable activities or patterns
3. Any progression or evolution in the content

Keep the summary to 3-5 bullet points. Be specific and actionable. Avoid generic statements.";

    public LLMMemorySummarizer(IChatClient chatClient)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    }

    /// <summary>
    /// Generates a conversational summary using the configured LLM.
    /// </summary>
    public async ValueTask<string?> SummarizeAsync(GetResult memories, CancellationToken ct = default)
    {
        if (memories.Documents.Count == 0)
        {
            return null;
        }

        // Cost control: limit to 50 memories max
        var memoriesToSummarize = memories.Documents.Take(50).ToList();
        
        // Build user prompt with chronological memory list
        var userPrompt = "Recent memories:\n\n";
        for (int i = 0; i < memoriesToSummarize.Count; i++)
        {
            userPrompt += $"{i + 1}. {memoriesToSummarize[i]}\n";
        }

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, userPrompt)
        };

        var options = new ChatOptions
        {
            MaxOutputTokens = 512,
            Temperature = 0.7f
        };

        try
        {
            var response = await _chatClient.GetResponseAsync(messages, options, ct);
            return response.Text;
        }
        catch (Exception ex)
        {
            // Graceful degradation: log and return null
            // In production, consider logging via ILogger
            Console.Error.WriteLine($"[WARN] LLM summarization failed: {ex.Message}");
            return null;
        }
    }
}
