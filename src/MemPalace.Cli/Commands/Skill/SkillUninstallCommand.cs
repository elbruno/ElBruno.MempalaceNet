using System.ComponentModel;
using MemPalace.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Commands.Skill;

internal sealed class SkillUninstallSettings : CommandSettings
{
    [CommandArgument(0, "<skill-id>")]
    [Description("Skill identifier")]
    public string SkillId { get; init; } = string.Empty;

    [CommandOption("--force")]
    [Description("Skip confirmation prompt")]
    [DefaultValue(false)]
    public bool Force { get; init; }
}

internal sealed class SkillUninstallCommand : AsyncCommand<SkillUninstallSettings>
{
    private readonly SkillManager _skillManager;

    public SkillUninstallCommand(SkillManager skillManager)
    {
        _skillManager = skillManager;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, SkillUninstallSettings settings)
    {
        // Check if skill exists
        var skill = _skillManager.GetInfo(settings.SkillId);
        if (skill == null)
        {
            AnsiConsole.MarkupLine($"[red]Skill '[blue]{settings.SkillId}[/]' not found.[/]");
            return 1;
        }

        // Confirm unless --force
        if (!settings.Force)
        {
            var confirm = AnsiConsole.Confirm(
                $"Are you sure you want to uninstall '{skill.Name}' ({skill.Id})?", 
                defaultValue: false);
            
            if (!confirm)
            {
                AnsiConsole.MarkupLine("[yellow]Uninstall cancelled.[/]");
                return 0;
            }
        }

        var success = _skillManager.Uninstall(settings.SkillId);

        if (!success)
        {
            AnsiConsole.MarkupLine($"[red]Failed to uninstall skill '[blue]{settings.SkillId}[/]'.[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[green]✓ Skill '[blue]{settings.SkillId}[/]' uninstalled successfully![/]");
        
        await Task.CompletedTask;
        return 0;
    }
}
