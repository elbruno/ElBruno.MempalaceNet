using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Commands.Skill;

internal sealed class SkillMarketplaceSearchSettings : CommandSettings
{
    [CommandArgument(0, "<query>")]
    [Description("Search query for marketplace skills")]
    public string Query { get; init; } = string.Empty;
}

/// <summary>
/// Search remote skill marketplace (requires MCP server connection).
/// Phase 2: Graceful fallback to local search if MCP unavailable.
/// </summary>
internal sealed class SkillMarketplaceSearchCommand : AsyncCommand<SkillMarketplaceSearchSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SkillMarketplaceSearchSettings settings)
    {
        // Phase 2: MCP integration pending (Roy's workstream)
        // For now, show helpful error message with remediation
        
        var panel = new Panel(
            "[yellow]Remote marketplace search is not yet available.[/]\n\n" +
            "[dim]This feature requires the MCP server to be running with skill marketplace tools enabled.[/]\n\n" +
            "[white]Remediation steps:[/]\n" +
            "1. Check if your Palace server is running: [cyan]mempalacenet mcp --transport sse[/]\n" +
            "2. Verify marketplace tools are registered (coming in Phase 2 Workstream B)\n" +
            "3. For now, use local search: [cyan]mempalacenet skill search " + Markup.Escape(settings.Query) + "[/]\n\n" +
            "[dim]For more information, see: docs/guides/skill-marketplace.md[/]"
        )
        {
            Header = new PanelHeader("[red]Feature Not Yet Available[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Yellow)
        };
        
        AnsiConsole.Write(panel);
        
        await Task.CompletedTask;
        return 1;
    }
}
