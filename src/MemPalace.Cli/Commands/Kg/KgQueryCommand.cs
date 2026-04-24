using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Commands.Kg;

internal sealed class KgQuerySettings : CommandSettings
{
    [CommandArgument(0, "<pattern>")]
    [Description("Query pattern (e.g., '? worked-on MemPalace.CLI')")]
    public string Pattern { get; init; } = string.Empty;

    [CommandOption("--at")]
    [Description("Query as of specific time (ISO 8601)")]
    public string? At { get; init; }
}

internal sealed class KgQueryCommand : AsyncCommand<KgQuerySettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, KgQuerySettings settings)
    {
        var panel = new Panel($"[yellow]TODO(phase6): implementation pending[/]\n\nPattern: [blue]{settings.Pattern}[/]\nAt time: [blue]{settings.At ?? "(current)"}[/]")
        {
            Header = new PanelHeader("[bold green]mempalacenet kg query[/]"),
            Border = BoxBorder.Rounded
        };
        
        AnsiConsole.Write(panel);
        
        // Stub results
        var table = new Table();
        table.AddColumn("Subject");
        table.AddColumn("Predicate");
        table.AddColumn("Object");
        table.AddColumn("Valid From");
        
        table.AddRow("Tyrell", "worked-on", "MemPalace.Core", "2026-04-24");
        table.AddRow("Roy", "worked-on", "MemPalace.Ai", "2026-04-24");
        table.AddRow("Rachael", "worked-on", "MemPalace.Cli", "2026-04-24");
        
        AnsiConsole.Write(table);
        
        await Task.CompletedTask;
        return 0;
    }
}
