using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CustomSkill;

/// <summary>
/// CLI command for executing the custom skill.
/// Demonstrates Spectre.Console integration and result display.
/// </summary>
[Description("Execute custom skill with semantic search")]
public class CustomSkillCommand : AsyncCommand<CustomSkillSettings>
{
    private readonly ICustomSkillService _skillService;

    public CustomSkillCommand(ICustomSkillService skillService)
    {
        _skillService = skillService;
    }

    public override async Task<int> ExecuteAsync(CommandContext ctx, CustomSkillSettings settings)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(settings.Query))
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Query cannot be empty");
                return 1;
            }

            // Execute the skill
            AnsiConsole.MarkupLine($"[bold cyan]Custom Skill[/] executing query: [yellow]\"{settings.Query}\"[/]");
            
            var result = await _skillService.ExecuteAsync(
                query: settings.Query,
                wing: settings.Wing,
                limit: settings.Limit
            );

            // Display results
            DisplayResults(result);

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            if (settings.Verbose)
            {
                AnsiConsole.WriteException(ex);
            }
            return 1;
        }
    }

    /// <summary>
    /// Displays skill results in a formatted table.
    /// </summary>
    private static void DisplayResults(CustomSkillResult result)
    {
        AnsiConsole.WriteLine();

        // Summary
        var summary = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Property")
            .AddColumn("Value");

        summary.AddRow("Query", $"[yellow]{result.Query}[/]");
        summary.AddRow("Wing", $"[cyan]{result.Wing}[/]");
        summary.AddRow("Results", $"[green]{result.Items.Length}[/]");
        summary.AddRow("Timestamp", result.Timestamp.ToString("o"));

        AnsiConsole.Write(summary);
        AnsiConsole.WriteLine();

        if (result.Items.Length == 0)
        {
            AnsiConsole.MarkupLine("[dim]No results found[/]");
            return;
        }

        // Results table
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold]Results ({result.Items.Length})[/]")
            .AddColumn("Score", col => col.Alignment = Justify.Right)
            .AddColumn("Content")
            .AddColumn("Wing");

        foreach (var item in result.Items)
        {
            var scoreColor = item.Score switch
            {
                >= 0.9f => "green",
                >= 0.8f => "yellow",
                _ => "dim"
            };

            var scoreDisplay = $"[{scoreColor}]{item.Score:F3}[/]";
            var contentPreview = item.Content.Length > 50
                ? item.Content.Substring(0, 50) + "..."
                : item.Content;

            var wing = item.Metadata.TryGetValue("wing", out var wingValue)
                ? wingValue.ToString() ?? "?"
                : "?";

            table.AddRow(
                scoreDisplay,
                $"[dim]{contentPreview}[/]",
                $"[cyan]{wing}[/]"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        // Footer
        AnsiConsole.MarkupLine("[dim]Tip: Use [yellow]--wing[/] to limit results to a specific wing[/]");
    }
}

/// <summary>
/// Settings for the custom skill command.
/// </summary>
public class CustomSkillSettings : CommandSettings
{
    [CommandArgument(0, "[QUERY]")]
    [Description("The search query to execute")]
    public string Query { get; set; } = string.Empty;

    [CommandOption("--wing <WING>")]
    [Description("Target wing (memory category)")]
    public string? Wing { get; set; }

    [CommandOption("--limit <N>")]
    [Description("Maximum number of results")]
    [DefaultValue(10)]
    public int Limit { get; set; } = 10;

    [CommandOption("-v|--verbose")]
    [Description("Enable verbose output")]
    public bool Verbose { get; set; }
}
