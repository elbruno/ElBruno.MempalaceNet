using MemPalace.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Commands.Skill;

internal sealed class SkillListSettings : CommandSettings
{
}

internal sealed class SkillListCommand : AsyncCommand<SkillListSettings>
{
    private readonly SkillManager _skillManager;

    public SkillListCommand(SkillManager skillManager)
    {
        _skillManager = skillManager;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, SkillListSettings settings)
    {
        var skills = _skillManager.List();

        if (skills.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No skills installed. Use 'mempalacenet skill install <path>' to install a skill.[/]");
            return 0;
        }

        var table = new Table();
        table.AddColumn("ID");
        table.AddColumn("Name");
        table.AddColumn("Version");
        table.AddColumn("Enabled");
        table.AddColumn("Description");
        
        foreach (var skill in skills)
        {
            var enabledIcon = skill.Enabled ? "[green]✓[/]" : "[dim]✗[/]";
            table.AddRow(
                skill.Id,
                skill.Name,
                skill.Version,
                enabledIcon,
                skill.Description.Length > 50 
                    ? skill.Description[..47] + "..." 
                    : skill.Description);
        }
        
        AnsiConsole.Write(table);
        
        await Task.CompletedTask;
        return 0;
    }
}
