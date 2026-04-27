using System.ComponentModel;
using MemPalace.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Commands.Skill;

internal sealed class SkillDisableSettings : CommandSettings
{
    [CommandArgument(0, "<skill-id>")]
    [Description("Skill identifier")]
    public string SkillId { get; init; } = string.Empty;
}

internal sealed class SkillDisableCommand : AsyncCommand<SkillDisableSettings>
{
    private readonly SkillManager _skillManager;

    public SkillDisableCommand(SkillManager skillManager)
    {
        _skillManager = skillManager;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, SkillDisableSettings settings)
    {
        var success = await _skillManager.DisableAsync(settings.SkillId);

        if (!success)
        {
            AnsiConsole.MarkupLine($"[red]Skill '[blue]{settings.SkillId}[/]' not found.[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[yellow]Skill '[blue]{settings.SkillId}[/]' disabled.[/]");
        return 0;
    }
}
