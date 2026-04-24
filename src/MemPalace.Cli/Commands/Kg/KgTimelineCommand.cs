using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Commands.Kg;

internal sealed class KgTimelineSettings : CommandSettings
{
    [CommandArgument(0, "<entity>")]
    [Description("Entity to view timeline for")]
    public string Entity { get; init; } = string.Empty;

    [CommandOption("--from")]
    [Description("Start time (ISO 8601)")]
    public string? From { get; init; }

    [CommandOption("--to")]
    [Description("End time (ISO 8601)")]
    public string? To { get; init; }
}

internal sealed class KgTimelineCommand : AsyncCommand<KgTimelineSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, KgTimelineSettings settings)
    {
        var panel = new Panel($"[yellow]TODO(phase6): implementation pending[/]\n\nEntity: [blue]{settings.Entity}[/]\nFrom: [blue]{settings.From ?? "(earliest)"}[/]\nTo: [blue]{settings.To ?? "(latest)"}[/]")
        {
            Header = new PanelHeader("[bold green]mempalacenet kg timeline[/]"),
            Border = BoxBorder.Rounded
        };
        
        AnsiConsole.Write(panel);
        
        // Stub timeline
        var table = new Table();
        table.AddColumn("Time");
        table.AddColumn("Event");
        table.AddColumn("Details");
        
        table.AddRow("2026-04-24 10:00", "started-working-on", "MemPalace.Core (Phase 1)");
        table.AddRow("2026-04-24 14:30", "completed", "Backend interfaces");
        table.AddRow("2026-04-24 15:00", "started-working-on", "SQLite backend (Phase 2)");
        
        AnsiConsole.Write(table);
        
        await Task.CompletedTask;
        return 0;
    }
}
