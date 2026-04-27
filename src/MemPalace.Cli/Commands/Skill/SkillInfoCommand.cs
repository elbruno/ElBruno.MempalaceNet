using System.ComponentModel;
using MemPalace.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Commands.Skill;

internal sealed class SkillInfoSettings : CommandSettings
{
    [CommandArgument(0, "<skill-id>")]
    [Description("Skill identifier")]
    public string SkillId { get; init; } = string.Empty;
}

internal sealed class SkillInfoCommand : AsyncCommand<SkillInfoSettings>
{
    private readonly SkillManager _skillManager;

    public SkillInfoCommand(SkillManager skillManager)
    {
        _skillManager = skillManager;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, SkillInfoSettings settings)
    {
        var skill = _skillManager.GetInfo(settings.SkillId);

        if (skill == null)
        {
            AnsiConsole.MarkupLine($"[red]Skill '[blue]{settings.SkillId}[/]' not found.[/]");
            return 1;
        }

        var panel = new Panel(
            $"[bold]{skill.Name}[/] v{skill.Version}\n\n" +
            $"{skill.Description}\n\n" +
            $"[dim]ID:[/] {skill.Id}\n" +
            $"[dim]Author:[/] {skill.Author ?? "Unknown"}\n" +
            $"[dim]Entry Point:[/] {skill.EntryPoint}\n" +
            $"[dim]Enabled:[/] {(skill.Enabled ? "[green]Yes[/]" : "[red]No[/]")}\n" +
            $"[dim]Tags:[/] {string.Join(", ", skill.Tags)}\n" +
            (skill.Repository != null ? $"[dim]Repository:[/] {skill.Repository}\n" : "") +
            (skill.License != null ? $"[dim]License:[/] {skill.License}\n" : "") +
            (skill.Dependencies.Count > 0 
                ? $"\n[bold]Dependencies:[/]\n{string.Join("\n", skill.Dependencies.Select(d => $"  • {d.Key} ({d.Value})"))}" 
                : ""))
        {
            Header = new PanelHeader($"[bold green]Skill Info: {skill.Id}[/]"),
            Border = BoxBorder.Rounded
        };
        
        AnsiConsole.Write(panel);
        
        await Task.CompletedTask;
        return 0;
    }
}
