using MemPalace.Agents.Diary;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace MemPalace.Cli.Commands.Agents;

internal sealed class AgentsDiarySettings : CommandSettings
{
    [CommandArgument(0, "<agent-id>")]
    [Description("The ID of the agent whose diary to view")]
    public string AgentId { get; set; } = string.Empty;

    [CommandOption("--tail <N>")]
    [Description("Number of recent entries to show")]
    public int? Tail { get; set; }

    [CommandOption("--search <query>")]
    [Description("Search diary entries")]
    public string? Search { get; set; }
}

internal sealed class AgentsDiaryCommand : AsyncCommand<AgentsDiarySettings>
{
    private readonly IAgentDiary _diary;

    public AgentsDiaryCommand(IAgentDiary diary)
    {
        _diary = diary;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, AgentsDiarySettings settings)
    {
        try
        {
            IReadOnlyList<DiaryEntry> entries;

            if (!string.IsNullOrEmpty(settings.Search))
            {
                entries = await _diary.SearchAsync(settings.AgentId, settings.Search, topK: settings.Tail ?? 10);
                AnsiConsole.MarkupLine($"[bold]Search results for '{settings.Search}':[/]");
            }
            else
            {
                entries = await _diary.RecentAsync(settings.AgentId, take: settings.Tail ?? 50);
                AnsiConsole.MarkupLine($"[bold]Recent diary entries for {settings.AgentId}:[/]");
            }

            if (entries.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No diary entries found.[/]");
                return 0;
            }

            foreach (var entry in entries)
            {
                var roleColor = entry.Role == "user" ? "cyan" : "green";
                AnsiConsole.MarkupLine($"[dim]{entry.At:yyyy-MM-dd HH:mm:ss}[/] [{roleColor}]{entry.Role}:[/] {entry.Content}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }
}
