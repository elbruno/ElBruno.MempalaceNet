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
    
    [CommandOption("--collection")]
    [Description("Collection name")]
    [DefaultValue("memories")]
    public string Collection { get; init; } = "memories";
}

internal sealed class SearchCommand : AsyncCommand<SearchSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SearchSettings settings)
    {
        var panel = new Panel($"[yellow]Search implementation ready[/]\n\nQuery: [blue]{settings.Query}[/]\nWing: [blue]{settings.Wing ?? "(all)"}[/]\nRerank: [blue]{settings.Rerank}[/]\nTop-K: [blue]{settings.TopK}[/]\nCollection: [blue]{settings.Collection}[/]\n\n[dim]Note: Full search requires backend and embedder configured (Phase 2+3)[/]")
        {
            Header = new PanelHeader("[bold green]mempalacenet search[/]"),
            Border = BoxBorder.Rounded
        };
        
        AnsiConsole.Write(panel);
        
        // Show progress bar if reranking is enabled
        if (settings.Rerank)
        {
            await AnsiConsole.Progress()
                .AutoRefresh(true)
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(new ProgressColumn[] 
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new RemainingTimeColumn(),
                    new SpinnerColumn(),
                })
                .StartAsync(async ctx =>
                {
                    var searchTask = ctx.AddTask("[green]Vector search[/]", maxValue: 100);
                    for (int i = 0; i <= 100; i += 20)
                    {
                        await Task.Delay(50);
                        searchTask.Value = i;
                    }
                    searchTask.StopTask();
                    
                    var rerankTask = ctx.AddTask("[yellow]LLM reranking[/]", maxValue: settings.TopK);
                    for (int i = 0; i < settings.TopK; i++)
                    {
                        await Task.Delay(100);
                        rerankTask.Increment(1);
                        rerankTask.Description = $"[yellow]LLM reranking[/] ({i + 1}/{settings.TopK})";
                    }
                    rerankTask.StopTask();
                });
            
            AnsiConsole.WriteLine();
        }
        
        // Stub results table
        var table = new Table();
        table.AddColumn("Score");
        table.AddColumn("Wing");
        table.AddColumn("Memory");
        table.AddRow("0.95", "conversations", "Discussion about vector databases...");
        table.AddRow("0.87", "code", "Implementation of search algorithm...");
        table.AddRow("0.82", "conversations", "Planning session for CLI design...");
        
        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("\n[dim]Showing example results (DI wired, awaiting backend/embedder)[/]");
        
        await Task.CompletedTask;
        return 0;
    }
}
