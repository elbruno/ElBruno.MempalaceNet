using System.ComponentModel;
using MemPalace.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Commands.Skill;

internal sealed class SkillEnableSettings : CommandSettings
{
    [CommandArgument(0, "<skill-id>")]
    [Description("Skill identifier")]
    public string SkillId { get; init; } = string.Empty;
}

internal sealed class SkillEnableCommand : AsyncCommand<SkillEnableSettings>
{
    private readonly SkillManager _skillManager;

    public SkillEnableCommand(SkillManager skillManager)
    {
        _skillManager = skillManager;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, SkillEnableSettings settings)
    {
        var success = await _skillManager.EnableAsync(settings.SkillId);

        if (!success)
        {
            AnsiConsole.MarkupLine($"[red]Skill '[blue]{settings.SkillId}[/]' not found.[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[green]✓ Skill '[blue]{settings.SkillId}[/]' enabled successfully![/]");
        return 0;
    }
}
