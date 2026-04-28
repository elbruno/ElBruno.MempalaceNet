using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using MemPalace.Cli.Output;
using MemPalace.Core.Services;
using MemPalace.Core.Backends;

namespace MemPalace.Cli.Commands;

internal sealed class WakeUpSettings : CommandSettings
{
    [CommandOption("--wing")]
    [Description("Limit to a specific wing")]
    public string? Wing { get; init; }
    
    [CommandOption("--limit")]
    [Description("Maximum number of recent memories to display")]
    [DefaultValue(20)]
    public int Limit { get; init; } = 20;
    
    [CommandOption("--collection")]
    [Description("Collection name")]
    [DefaultValue("memories")]
    public string Collection { get; init; } = "memories";
    
    [CommandOption("--summarize")]
    [Description("Generate a summary of recent memories")]
    [DefaultValue(false)]
    public bool Summarize { get; init; }
}

internal sealed class WakeUpCommand : AsyncCommand<WakeUpSettings>
{
    private readonly IBackend? _backend;
    private readonly IWakeUpService? _wakeUpService;

    public WakeUpCommand(IBackend? backend = null, IWakeUpService? wakeUpService = null)
    {
        _backend = backend;
        _wakeUpService = wakeUpService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, WakeUpSettings settings)
    {
        try
        {
            if (_backend == null || _wakeUpService == null)
            {
                ErrorFormatter.DisplayGenericError(
                    "Backend or WakeUpService not configured. " +
                    "Please ensure MemPalace is properly initialized.");
                return 1;
            }

            var panel = OutputFormatter.CreatePanel(
                "mempalacenet wake-up",
                $"Wing: [blue]{settings.Wing ?? "(all)"}[/]\n" +
                $"Limit: [blue]{settings.Limit}[/]\n" +
                $"Collection: [blue]{settings.Collection}[/]\n" +
                $"Summarize: [blue]{settings.Summarize}[/]");
            
            AnsiConsole.Write(panel);

            // Get or create collection
            ICollection collection;
            try
            {
                collection = await _backend.GetOrCreateCollectionAsync(
                    settings.Collection,
                    dimensions: 384, // Default dimension (will be ignored if collection exists)
                    embedderIdentity: "local", // Default embedder
                    ct: default);
            }
            catch (Exception ex)
            {
                ErrorFormatter.DisplayGenericError($"Failed to access collection '{settings.Collection}': {ex.Message}");
                return 1;
            }

            // Build where clause if wing is specified
            WhereClause? whereClause = settings.Wing != null
                ? new WhereClause.Eq("wing", settings.Wing)
                : null;

            // Retrieve and optionally summarize memories
            var result = await _wakeUpService.WakeUpAsync(
                collection,
                limit: settings.Limit,
                where: whereClause,
                summarize: settings.Summarize);

            if (result.Memories.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No memories found.[/]");
                return 0;
            }

            // Display summary if available
            if (result.Summary != null)
            {
                var summaryPanel = new Panel(result.Summary)
                {
                    Header = new PanelHeader("[bold cyan]Summary[/]"),
                    Border = BoxBorder.Rounded
                };
                AnsiConsole.Write(summaryPanel);
                AnsiConsole.WriteLine();
            }

            // Display memories as a tree
            var tree = new Tree("[bold cyan]Recent Memories[/]");
            
            var groupedByWing = result.Memories
                .GroupBy(m => m.Metadata.TryGetValue("wing", out var w) ? w?.ToString() : "(no wing)");
            
            foreach (var wingGroup in groupedByWing)
            {
                var wingNode = tree.AddNode($"[yellow]{wingGroup.Key}[/]");
                
                foreach (var memory in wingGroup)
                {
                    var timestamp = memory.Metadata.TryGetValue("timestamp", out var ts)
                        ? ParseTimestamp(ts)
                        : DateTime.MinValue;
                    var timeStr = timestamp != DateTime.MinValue
                        ? OutputFormatter.FormatTimestamp(timestamp)
                        : "unknown";
                    var room = memory.Metadata.TryGetValue("room", out var r) ? r?.ToString() : "";
                    var roomStr = !string.IsNullOrEmpty(room) ? $"[cyan]{room}[/]: " : "";
                    wingNode.AddNode($"[dim]{timeStr}[/] {roomStr}{OutputFormatter.Truncate(memory.Document, 60)}");
                }
            }
            
            AnsiConsole.Write(tree);

            // Display summary stats
            var statsTable = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Metric")
                .AddColumn(new TableColumn("Value").RightAligned());
            
            statsTable.AddRow("Total memories", result.TotalCount.ToString("N0"));
            statsTable.AddRow("Displayed", result.Memories.Count.ToString());
            
            var wings = result.Memories
                .Select(m => m.Metadata.TryGetValue("wing", out var w) ? w?.ToString() : null)
                .Where(w => w != null)
                .Distinct()
                .Count();
            statsTable.AddRow("Wings", wings.ToString());
            
            if (result.Memories.Count > 0)
            {
                var latestTimestamp = result.Memories
                    .Select(m => m.Metadata.TryGetValue("timestamp", out var ts) ? ParseTimestamp(ts) : DateTime.MinValue)
                    .Max();
                if (latestTimestamp != DateTime.MinValue)
                {
                    statsTable.AddRow("Last activity", OutputFormatter.FormatTimestamp(latestTimestamp));
                }
            }
            
            AnsiConsole.Write(new Panel(statsTable)
            {
                Header = new PanelHeader("[bold]Palace Statistics[/]"),
                Border = BoxBorder.Rounded
            });
            
            OutputFormatter.DisplaySuccess($"Retrieved {result.Memories.Count} recent memories");
            
            return 0;
        }
        catch (Exception ex)
        {
            ErrorFormatter.DisplayGenericError($"Failed to wake up palace: {ex.Message}");
            return 1;
        }
    }

    private static DateTime ParseTimestamp(object? value)
    {
        if (value == null) return DateTime.MinValue;
        
        if (value is DateTime dt) return dt;
        if (value is DateTimeOffset dto) return dto.UtcDateTime;
        if (value is string str && DateTime.TryParse(str, out var parsed)) return parsed;
        if (value is long ticks) return new DateTime(ticks, DateTimeKind.Utc);
        
        return DateTime.MinValue;
    }
}
