using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using MemPalace.Cli.Output;

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
}

internal sealed class WakeUpCommand : AsyncCommand<WakeUpSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, WakeUpSettings settings)
    {
        try
        {
            var panel = OutputFormatter.CreatePanel(
                "mempalacenet wake-up",
                $"Wing: [blue]{settings.Wing ?? "(all)"}[/]\n" +
                $"Limit: [blue]{settings.Limit}[/]\n" +
                $"Collection: [blue]{settings.Collection}[/]");
            
            AnsiConsole.Write(panel);
            
            // Simulate fetching recent memories
            var memories = new[]
            {
                (DateTime.UtcNow.AddHours(-2), "conversations", "planning", "Discussion about CLI UX improvements and progress bars"),
                (DateTime.UtcNow.AddHours(-5), "code", "core", "Implemented error formatter with remediation steps"),
                (DateTime.UtcNow.AddDays(-1), "docs", "architecture", "Updated MCP tool catalog documentation"),
                (DateTime.UtcNow.AddDays(-2), "conversations", "design", "Team meeting on knowledge graph temporal queries"),
                (DateTime.UtcNow.AddDays(-3), "code", "mining", "Added file mining with gitignore support")
            }.Take(settings.Limit).ToArray();
            
            // Display as a tree for better visual hierarchy
            var tree = new Tree("[bold cyan]Recent Memories[/]");
            
            var groupedByWing = memories.GroupBy(m => m.Item2);
            foreach (var wingGroup in groupedByWing)
            {
                var wingNode = tree.AddNode($"[yellow]{wingGroup.Key}[/]");
                
                foreach (var (timestamp, _, room, content) in wingGroup)
                {
                    var timeStr = OutputFormatter.FormatTimestamp(timestamp);
                    wingNode.AddNode($"[dim]{timeStr}[/] [cyan]{room}[/]: {OutputFormatter.Truncate(content, 60)}");
                }
            }
            
            AnsiConsole.Write(tree);
            
            // Display summary stats
            var statsTable = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Metric")
                .AddColumn(new TableColumn("Value").RightAligned());
            
            statsTable.AddRow("Total memories", "1,247");
            statsTable.AddRow("Active wings", "5");
            statsTable.AddRow("Last activity", OutputFormatter.FormatTimestamp(memories.First().Item1));
            statsTable.AddRow("Most active wing", "conversations (487 memories)");
            
            AnsiConsole.Write(new Panel(statsTable)
            {
                Header = new PanelHeader("[bold]Palace Statistics[/]"),
                Border = BoxBorder.Rounded
            });
            
            OutputFormatter.DisplaySuccess($"Retrieved {memories.Length} recent memories");
            
            await Task.CompletedTask;
            return 0;
        }
        catch (Exception ex)
        {
            ErrorFormatter.DisplayGenericError($"Failed to wake up palace: {ex.Message}");
            return 1;
        }
    }
}
