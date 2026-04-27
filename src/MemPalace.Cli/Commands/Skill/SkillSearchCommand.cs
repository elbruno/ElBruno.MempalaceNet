using System.ComponentModel;
using MemPalace.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Commands.Skill;

internal sealed class SkillSearchSettings : CommandSettings
{
    [CommandArgument(0, "<query>")]
    [Description("Search query")]
    public string Query { get; init; } = string.Empty;
}

internal sealed class SkillSearchCommand : AsyncCommand<SkillSearchSettings>
{
    private readonly SkillManager _skillManager;

    public SkillSearchCommand(SkillManager skillManager)
    {
        _skillManager = skillManager;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, SkillSearchSettings settings)
    {
        var skills = _skillManager.Search(settings.Query);

        if (skills.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]No skills found matching '[blue]{settings.Query}[/]'[/]");
            AnsiConsole.MarkupLine("[dim]Note: Phase 1 searches local skills only. Remote registry coming in Phase 2.[/]");
            return 0;
        }

        var table = new Table();
        table.AddColumn("ID");
        table.AddColumn("Name");
        table.AddColumn("Version");
        table.AddColumn("Tags");
        table.AddColumn("Description");
        
        foreach (var skill in skills)
        {
            table.AddRow(
                skill.Id,
                skill.Name,
                skill.Version,
                string.Join(", ", skill.Tags),
                skill.Description.Length > 40 
                    ? skill.Description[..37] + "..." 
                    : skill.Description);
        }
        
        AnsiConsole.Write(table);
        
        await Task.CompletedTask;
        return 0;
    }
}
