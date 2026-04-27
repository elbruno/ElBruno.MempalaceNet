using System.ComponentModel;
using MemPalace.Ai.Summarization;
using MemPalace.Core.Backends;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Commands;

internal sealed class WakeUpSettings : CommandSettings
{
    [CommandOption("--days")]
    [Description("Number of days to look back")]
    [DefaultValue(7)]
    public int Days { get; init; } = 7;

    [CommandOption("--wing")]
    [Description("Filter by specific wing")]
    public string? Wing { get; init; }

    [CommandOption("--limit")]
    [Description("Maximum number of memories to fetch")]
    [DefaultValue(100)]
    public int Limit { get; init; } = 100;

    [CommandOption("--collection")]
    [Description("Collection name")]
    [DefaultValue("memories")]
    public string Collection { get; init; } = "memories";
}

internal sealed class WakeUpCommand : AsyncCommand<WakeUpSettings>
{
    private readonly IBackend _backend;
    private readonly IMemorySummarizer _summarizer;

    public WakeUpCommand(IBackend backend, IMemorySummarizer summarizer)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _summarizer = summarizer ?? throw new ArgumentNullException(nameof(summarizer));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, WakeUpSettings settings)
    {
        var palaceRef = new Core.Model.PalaceRef("default");

        try
        {
            // Get collection
            var collection = await _backend.GetCollectionAsync(
                palaceRef,
                settings.Collection,
                create: false);

            // Fetch recent memories (last N days)
            var cutoffDate = DateTime.UtcNow.AddDays(-settings.Days);
            
            // Build where clause for date filtering (if backend supports timestamp metadata)
            // For now, fetch all and filter client-side
            var getResult = await collection.GetAsync(
                ids: null,
                where: null,
                limit: settings.Limit,
                offset: 0,
                include: IncludeFields.Documents | IncludeFields.Metadatas);

            // Filter by wing if specified (check metadata)
            var filteredIds = new List<string>();
            var filteredDocs = new List<string>();
            var filteredMeta = new List<IReadOnlyDictionary<string, object?>>();

            for (int i = 0; i < getResult.Ids.Count; i++)
            {
                var metadata = getResult.Metadatas[i];
                
                // Filter by wing if specified
                if (settings.Wing != null && 
                    metadata.TryGetValue("wing", out var wingValue) && 
                    wingValue?.ToString() != settings.Wing)
                {
                    continue;
                }

                // Filter by timestamp if available
                if (metadata.TryGetValue("timestamp", out var tsValue) && 
                    DateTime.TryParse(tsValue?.ToString(), out var timestamp))
                {
                    if (timestamp < cutoffDate)
                    {
                        continue;
                    }
                }

                filteredIds.Add(getResult.Ids[i]);
                filteredDocs.Add(getResult.Documents[i]);
                filteredMeta.Add(metadata);
            }

            var filteredResult = new GetResult(filteredIds, filteredDocs, filteredMeta);

            // Calculate metadata
            var lastSession = GetLastSessionTime(filteredMeta);
            var activeWings = GetActiveWings(filteredMeta);
            var memoryCount = filteredResult.Documents.Count;

            // Display metadata panel
            var metadataText = $"Last session: {FormatLastSession(lastSession)}\n";
            metadataText += $"Memory count: {memoryCount}\n";
            metadataText += $"Active wings: {string.Join(", ", activeWings)}";

            var metadataPanel = new Panel(metadataText)
            {
                Header = new PanelHeader("[bold cyan]Session Context[/]"),
                Border = BoxBorder.Rounded
            };
            AnsiConsole.Write(metadataPanel);
            AnsiConsole.WriteLine();

            // Generate summary
            string? summary = null;
            if (memoryCount > 0)
            {
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Generating summary...", async ctx =>
                    {
                        summary = await _summarizer.SummarizeAsync(filteredResult);
                    });
            }

            // Display summary panel
            var summaryText = summary ?? GenerateFallbackSummary(memoryCount);
            var summaryPanel = new Panel(summaryText)
            {
                Header = new PanelHeader("[bold green]Recent Activity Summary[/]"),
                Border = BoxBorder.Rounded
            };
            AnsiConsole.Write(summaryPanel);

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private static DateTime? GetLastSessionTime(IReadOnlyList<IReadOnlyDictionary<string, object?>> metadatas)
    {
        DateTime? latest = null;
        foreach (var metadata in metadatas)
        {
            if (metadata.TryGetValue("timestamp", out var tsValue) &&
                DateTime.TryParse(tsValue?.ToString(), out var timestamp))
            {
                if (latest == null || timestamp > latest.Value)
                {
                    latest = timestamp;
                }
            }
        }
        return latest;
    }

    private static HashSet<string> GetActiveWings(IReadOnlyList<IReadOnlyDictionary<string, object?>> metadatas)
    {
        var wings = new HashSet<string>();
        foreach (var metadata in metadatas)
        {
            if (metadata.TryGetValue("wing", out var wingValue) && wingValue != null)
            {
                wings.Add(wingValue.ToString()!);
            }
        }
        return wings;
    }

    private static string FormatLastSession(DateTime? lastSession)
    {
        if (lastSession == null)
        {
            return "(no recent activity)";
        }

        var ago = DateTime.UtcNow - lastSession.Value;
        if (ago.TotalDays >= 1)
        {
            return $"{lastSession.Value:yyyy-MM-dd HH:mm} ({ago.TotalDays:F1} days ago)";
        }
        else if (ago.TotalHours >= 1)
        {
            return $"{lastSession.Value:yyyy-MM-dd HH:mm} ({ago.TotalHours:F1} hours ago)";
        }
        else
        {
            return $"{lastSession.Value:yyyy-MM-dd HH:mm} ({ago.TotalMinutes:F0} minutes ago)";
        }
    }

    private static string GenerateFallbackSummary(int memoryCount)
    {
        if (memoryCount == 0)
        {
            return "[dim]No recent memories found in the specified time range.[/]\n\n" +
                   "Try increasing --days or check if memories were stored with timestamps.";
        }

        return $"[yellow]LLM summarization not configured[/]\n\n" +
               $"Found {memoryCount} memories in the specified time range.\n\n" +
               "To enable AI-powered summaries, register an IChatClient via DI.\n" +
               "See [cyan]docs/guides/wake-up-summarization.md[/] for configuration examples.";
    }
}
