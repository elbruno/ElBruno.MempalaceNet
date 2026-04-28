using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Commands.Skill;

internal sealed class SkillSourceListSettings : CommandSettings
{
}

/// <summary>
/// List configured skill marketplace sources.
/// Phase 2: Placeholder until marketplace configuration is implemented.
/// </summary>
internal sealed class SkillSourceListCommand : AsyncCommand<SkillSourceListSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SkillSourceListSettings settings)
    {
        var panel = new Panel(
            "[yellow]Skill marketplace sources are not yet configured.[/]\n\n" +
            "[dim]This feature requires marketplace configuration in ~/.palace/config.json[/]\n\n" +
            "[white]Planned sources for v1.0:[/]\n" +
            "• [cyan]mempalace-official[/] - Official MemPalace skill registry\n" +
            "• [cyan]community[/] - Community-contributed skills (GitHub-backed)\n" +
            "• [cyan]local[/] - Local filesystem skills (already working)\n\n" +
            "[dim]Current status: Only local filesystem skills are supported (Phase 1)[/]\n" +
            "[dim]Remote registry support is planned for Phase 3 / v1.0[/]"
        )
        {
            Header = new PanelHeader("[yellow]⚠ Feature Preview[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Yellow)
        };
        
        AnsiConsole.Write(panel);
        
        // Show local skills as fallback
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]For now, you can list locally installed skills:[/]");
        AnsiConsole.MarkupLine("[cyan]  mempalacenet skill list[/]");
        
        await Task.CompletedTask;
        return 0;
    }
}
