using MemPalace.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace MemPalace.Cli.Commands.Skill;

internal sealed class SkillListSettings : CommandSettings
{
    [CommandOption("--available")]
    [Description("Show available skills (installed + registry)")]
    public bool Available { get; init; }

    [CommandOption("--installed")]
    [Description("Show only installed skills")]
    public bool Installed { get; init; }

    [CommandOption("--enabled")]
    [Description("Show only enabled skills")]
    public bool Enabled { get; init; }

    [CommandOption("--disabled")]
    [Description("Show only disabled skills")]
    public bool Disabled { get; init; }
}

internal sealed class SkillListCommand : AsyncCommand<SkillListSettings>
{
    private readonly SkillManager _skillManager;
    private readonly SkillRegistry _skillRegistry;

    public SkillListCommand(SkillManager skillManager, SkillRegistry skillRegistry)
    {
        _skillManager = skillManager;
        _skillRegistry = skillRegistry;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, SkillListSettings settings)
    {
        List<MemPalace.Core.Model.SkillManifest> skills;

        if (settings.Available)
        {
            // Show all discoverable skills (union of installed + registry)
            var installedSkills = _skillManager.List();
            var registry = _skillRegistry.GetDiscoverableSkills();
            
            var dict = new Dictionary<string, MemPalace.Core.Model.SkillManifest>();
            foreach (var skill in installedSkills)
                dict[skill.Id] = skill;
            foreach (var skill in registry)
                if (!dict.ContainsKey(skill.Id))
                    dict[skill.Id] = skill;
            
            skills = dict.Values.ToList();
        }
        else if (settings.Installed)
        {
            skills = _skillManager.List().ToList();
        }
        else
        {
            // Default: show all installed skills
            skills = _skillManager.List().ToList();
        }

        // Apply status filters
        if (settings.Enabled)
            skills = skills.Where(s => s.Enabled).ToList();
        
        if (settings.Disabled)
            skills = skills.Where(s => !s.Enabled).ToList();

        if (skills.Count == 0)
        {
            var msg = settings.Available ? "No skills available." : "No skills installed.";
            AnsiConsole.MarkupLine($"[yellow]{msg}[/]");
            if (!settings.Available)
                AnsiConsole.MarkupLine("[dim]Discover available skills: [cyan]mempalacenet skill discover[/][/]");
            return 0;
        }

        var table = new Table();
        table.AddColumn("ID");
        table.AddColumn("Name");
        table.AddColumn("Version");
        table.AddColumn("Status");
        table.AddColumn("Description");
        
        var installedIds = new HashSet<string>(_skillManager.List().Select(s => s.Id));

        foreach (var skill in skills)
        {
            string statusIcon;
            if (installedIds.Contains(skill.Id))
            {
                statusIcon = skill.Enabled ? "[green]✅ Enabled[/]" : "[dim]⚠️ Disabled[/]";
            }
            else
            {
                statusIcon = "[yellow]Available[/]";
            }

            table.AddRow(
                skill.Id,
                skill.Name,
                skill.Version,
                statusIcon,
                skill.Description.Length > 50 
                    ? skill.Description[..47] + "..." 
                    : skill.Description);
        }
        
        AnsiConsole.Write(table);

        var summary = skills.Count == 1 ? "skill" : "skills";
        AnsiConsole.MarkupLine($"\n[dim]{skills.Count} {summary} listed[/]");
        
        await Task.CompletedTask;
        return 0;
    }
}
