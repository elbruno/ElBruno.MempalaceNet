using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Commands;

internal sealed class SearchSettings : CommandSettings
{
    [CommandArgument(0, "<query>")]
    [Description("Search query")]
    public string Query { get; init; } = string.Empty;

    [CommandOption("--wing")]
    [Description("Limit search to a specific wing")]
    public string? Wing { get; init; }

    [CommandOption("--rerank")]
    [Description("Enable LLM-based reranking")]
    [DefaultValue(false)]
    public bool Rerank { get; init; }

    [CommandOption("--top-k")]
    [Description("Number of results to return")]
    [DefaultValue(10)]
    public int TopK { get; init; } = 10;
}

internal sealed class SearchCommand : AsyncCommand<SearchSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SearchSettings settings)
    {
        var panel = new Panel($"[yellow]TODO(phase4): implementation pending[/]\n\nQuery: [blue]{settings.Query}[/]\nWing: [blue]{settings.Wing ?? "(all)"}[/]\nRerank: [blue]{settings.Rerank}[/]\nTop-K: [blue]{settings.TopK}[/]")
        {
            Header = new PanelHeader("[bold green]mempalacenet search[/]"),
            Border = BoxBorder.Rounded
        };
        
        AnsiConsole.Write(panel);
        
        // Stub results table
        var table = new Table();
        table.AddColumn("Score");
        table.AddColumn("Wing");
        table.AddColumn("Memory");
        table.AddRow("0.95", "conversations", "Discussion about vector databases...");
        table.AddRow("0.87", "code", "Implementation of search algorithm...");
        table.AddRow("0.82", "conversations", "Planning session for CLI design...");
        
        AnsiConsole.Write(table);
        
        await Task.CompletedTask;
        return 0;
    }
}
