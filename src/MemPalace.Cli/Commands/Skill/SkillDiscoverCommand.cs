using System.ComponentModel;
using MemPalace.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Commands.Skill;

internal sealed class SkillDiscoverSettings : CommandSettings
{
    [CommandOption("--tag <tag>")]
    [Description("Filter by tag (case-insensitive)")]
    public string? Tag { get; init; }

    [CommandOption("--limit <n>")]
    [Description("Maximum number of results (default: 10)")]
    public int Limit { get; init; } = 10;
}

internal sealed class SkillDiscoverCommand : AsyncCommand<SkillDiscoverSettings>
{
    private readonly SkillRegistry _skillRegistry;
    private readonly SkillManager _skillManager;

    public SkillDiscoverCommand(SkillRegistry skillRegistry, SkillManager skillManager)
    {
        _skillRegistry = skillRegistry;
        _skillManager = skillManager;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, SkillDiscoverSettings settings)
    {
        // Get discoverable skills (from registry)
        var discoverable = _skillRegistry.GetDiscoverableSkills();

        // Filter by tag if provided
        if (!string.IsNullOrWhiteSpace(settings.Tag))
        {
            discoverable = discoverable
                .Where(s => s.Tags.Any(t => t.Equals(settings.Tag, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        // Apply limit
        discoverable = discoverable.Take(settings.Limit).ToList();

        if (discoverable.Count == 0)
        {
            var message = string.IsNullOrWhiteSpace(settings.Tag)
                ? "[yellow]No skills available in registry.[/]"
                : $"[yellow]No skills found with tag '[blue]{settings.Tag}[/]'[/]";

            AnsiConsole.MarkupLine(message);
            AnsiConsole.MarkupLine("[dim]Note: Phase 3 MVP includes 4 built-in demo skills. More coming in v1.0![/]");
            return 0;
        }

        // Build table
        var table = new Table();
        table.AddColumn("ID");
        table.AddColumn("Name");
        table.AddColumn("Version");
        table.AddColumn("Status");
        table.AddColumn("Tags");
        table.AddColumn("Description");

        // Get installed skills for status checking
        var installed = _skillManager.List();
        var installedIds = new HashSet<string>(installed.Select(s => s.Id));

        foreach (var skill in discoverable)
        {
            var statusIcon = installedIds.Contains(skill.Id)
                ? (skill.Enabled ? "[green]✅ Installed[/]" : "[dim]⚠️ Disabled[/]")
                : "[yellow]Available[/]";

            var tagsStr = skill.Tags.Count > 0
                ? string.Join(", ", skill.Tags.Take(2)) + (skill.Tags.Count > 2 ? "..." : "")
                : "-";

            var descStr = skill.Description.Length > 40
                ? skill.Description[..37] + "..."
                : skill.Description;

            table.AddRow(
                skill.Id,
                skill.Name,
                skill.Version,
                statusIcon,
                tagsStr,
                descStr);
        }

        AnsiConsole.Write(table);

        // Summary
        var summaryMsg = settings.Tag != null
            ? $"\n[dim]Found {discoverable.Count} skill(s) with tag '{settings.Tag}'[/]"
            : $"\n[dim]Showing {discoverable.Count} of {_skillRegistry.GetDiscoverableSkills().Count} available skills[/]";

        AnsiConsole.MarkupLine(summaryMsg);
        AnsiConsole.MarkupLine("[dim]Tip: Use 'mempalacenet skill info <id>' for details, or 'mempalacenet skill install <id>' to install[/]");

        await Task.CompletedTask;
        return 0;
    }
}
