using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using MemPalace.Cli.Output;

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

    [CommandOption("--bm25")]
    [Description("Enable BM25 keyword-only search (instead of semantic)")]
    [DefaultValue(false)]
    public bool BM25 { get; init; }

    [CommandOption("--hybrid")]
    [Description("Enable hybrid search (semantic + keyword via RRF)")]
    [DefaultValue(false)]
    public bool Hybrid { get; init; }

    [CommandOption("--top-k")]
    [Description("Number of results to return")]
    [DefaultValue(10)]
    public int TopK { get; init; } = 10;
    
    [CommandOption("--collection")]
    [Description("Collection name")]
    [DefaultValue("memories")]
    public string Collection { get; init; } = "memories";
    
    [CommandOption("--verbose")]
    [Description("Enable verbose output")]
    [DefaultValue(false)]
    public bool Verbose { get; init; }
}

internal sealed class SearchCommand : AsyncCommand<SearchSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SearchSettings settings)
    {
        try
        {
            // Validate query
            if (string.IsNullOrWhiteSpace(settings.Query))
            {
                ErrorFormatter.DisplaySearchError("", "query cannot be empty");
                return 1;
            }

            // Determine search mode
            var searchMode = settings.BM25 ? "BM25 (keyword)" :
                            settings.Hybrid ? "Hybrid (semantic + keyword)" :
                            "Semantic";

            var panel = OutputFormatter.CreatePanel(
                "mempalacenet search",
                $"Query: [blue]{settings.Query}[/]\n" +
                $"Mode: [cyan]{searchMode}[/]\n" +
                $"Wing: [blue]{settings.Wing ?? "(all)"}[/]\n" +
                $"Rerank: [blue]{settings.Rerank}[/]\n" +
                $"Top-K: [blue]{settings.TopK}[/]\n" +
                $"Collection: [blue]{settings.Collection}[/]\n" +
                $"Verbose: [blue]{settings.Verbose}[/]");
            
            AnsiConsole.Write(panel);
            
            if (settings.Verbose)
            {
                if (settings.BM25)
                    AnsiConsole.MarkupLine("[dim]Executing BM25 keyword search...[/]");
                else if (settings.Hybrid)
                    AnsiConsole.MarkupLine("[dim]Executing hybrid search (semantic + keyword)...[/]");
                else
                    AnsiConsole.MarkupLine("[dim]Executing semantic search...[/]");
            }
            
            // Simulate search (stub results)
            var results = new[]
            {
                ("0.95", "conversations", "planning", "Discussion about vector databases and embedding strategies", DateTime.UtcNow.AddDays(-2)),
                ("0.87", "code", "core", "Implementation of search algorithm with cosine similarity", DateTime.UtcNow.AddDays(-5)),
                ("0.82", "conversations", "design", "Planning session for CLI design and user experience", DateTime.UtcNow.AddDays(-1)),
                ("0.78", "docs", "architecture", "Documentation on the hierarchical memory structure", DateTime.UtcNow.AddDays(-7)),
                ("0.75", "code", "mining", "File mining pipeline with progress tracking", DateTime.UtcNow.AddDays(-3))
            }.Take(settings.TopK).ToArray();
            
            // Apply reranking with progress if requested
            if (settings.Rerank)
            {
                if (settings.Verbose)
                {
                    AnsiConsole.MarkupLine("[dim]Applying LLM-based reranking...[/]");
                }
                
                await ProgressDisplay.WithRerankProgress(
                    results.Length,
                    async progress =>
                    {
                        for (int i = 0; i < results.Length; i++)
                        {
                            progress.Report(new ProgressDisplay.RerankProgress(
                                ProcessedResults: i + 1,
                                TotalResults: results.Length));
                            
                            await Task.Delay(100); // Simulate reranking API call
                        }
                        
                        return results;
                    });
            }
            
            // Display results in a table
            var table = OutputFormatter.CreateSearchResultsTable();
            
            foreach (var (score, wing, room, content, timestamp) in results)
            {
                table.AddRow(
                    score,
                    $"[cyan]{wing}[/]",
                    $"[dim]{room}[/]",
                    OutputFormatter.Truncate(content, 60),
                    $"[dim]{OutputFormatter.FormatTimestamp(timestamp)}[/]");
            }
            
            AnsiConsole.Write(table);
            
            OutputFormatter.DisplaySuccess($"Found {results.Length} results ({searchMode})");
            if (settings.Rerank)
            {
                AnsiConsole.MarkupLine("[dim]Results reranked by relevance[/]");
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            if (settings.Verbose)
            {
                ErrorFormatter.DisplayGenericError(ex.Message, ex.StackTrace ?? "");
            }
            else
            {
                ErrorFormatter.DisplaySearchError(settings.Query, ex.Message);
            }
            return 1;
        }
    }
}
