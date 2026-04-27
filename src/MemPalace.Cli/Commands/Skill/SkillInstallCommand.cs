using System.ComponentModel;
using MemPalace.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Commands.Skill;

internal sealed class SkillInstallSettings : CommandSettings
{
    [CommandArgument(0, "<source-path>")]
    [Description("Path to skill directory")]
    public string SourcePath { get; init; } = string.Empty;
}

internal sealed class SkillInstallCommand : AsyncCommand<SkillInstallSettings>
{
    private readonly SkillManager _skillManager;

    public SkillInstallCommand(SkillManager skillManager)
    {
        _skillManager = skillManager;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, SkillInstallSettings settings)
    {
        try
        {
            AnsiConsole.MarkupLine($"[blue]Installing skill from:[/] {settings.SourcePath}");
            
            await _skillManager.InstallAsync(settings.SourcePath);
            
            AnsiConsole.MarkupLine("[green]✓ Skill installed successfully![/]");
            AnsiConsole.MarkupLine("[dim]Use 'mempalacenet skill list' to see installed skills.[/]");
            
            return 0;
        }
        catch (DirectoryNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
        catch (FileNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            AnsiConsole.MarkupLine("[yellow]Ensure the source directory contains a valid skill.json manifest.[/]");
            return 1;
        }
        catch (InvalidDataException ex)
        {
            AnsiConsole.MarkupLine($"[red]Invalid manifest: {ex.Message}[/]");
            return 1;
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Unexpected error: {ex.Message}[/]");
            return 1;
        }
    }
}
