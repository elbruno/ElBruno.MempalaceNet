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
            var panel = new Panel(
                $"[red]Skill not found:[/] {Markup.Escape(settings.SkillId)}\n\n" +
                "[white]Remediation steps:[/]\n" +
                "1. List installed skills: [cyan]mempalacenet skill list[/]\n" +
                "2. Search for similar skills: [cyan]mempalacenet skill search " + Markup.Escape(settings.SkillId) + "[/]\n" +
                "3. Install the skill first: [cyan]mempalacenet skill install <path>[/]\n\n" +
                $"[dim]Hint: Skill IDs are case-sensitive. Common installed location: ~/.palace/skills/{Markup.Escape(settings.SkillId)}[/]"
            )
            {
                Header = new PanelHeader("[red]Skill Not Found[/]"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Red)
            };
            
            AnsiConsole.Write(panel);
            return 1;
        }

        var panel2 = new Panel(
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
        
        AnsiConsole.Write(panel2);
        
        await Task.CompletedTask;
        return 0;
    }
}
