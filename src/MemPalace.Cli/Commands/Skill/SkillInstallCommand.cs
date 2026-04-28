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
            AnsiConsole.Status()
                .Start("Installing skill...", ctx =>
                {
                    ctx.Status($"[blue]Validating source:[/] {settings.SourcePath}");
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("green"));
                });
            
            await _skillManager.InstallAsync(settings.SourcePath);
            
            AnsiConsole.MarkupLine("[green]✓ Skill installed successfully![/]");
            AnsiConsole.MarkupLine("[dim]Use 'mempalacenet skill list' to see installed skills.[/]");
            AnsiConsole.MarkupLine("[dim]Enable with: mempalacenet skill enable <skill-id>[/]");
            
            return 0;
        }
        catch (DirectoryNotFoundException ex)
        {
            var panel = new Panel(
                $"[red]Source path not found:[/] {Markup.Escape(settings.SourcePath)}\n\n" +
                "[white]Remediation steps:[/]\n" +
                "1. Verify the path exists and is accessible\n" +
                "2. Check for typos in the path\n" +
                "3. Try with an absolute path: [cyan]mempalacenet skill install /full/path/to/skill[/]\n\n" +
                $"[dim]Error details: {Markup.Escape(ex.Message)}[/]"
            )
            {
                Header = new PanelHeader("[red]Installation Failed[/]"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Red)
            };
            
            AnsiConsole.Write(panel);
            return 1;
        }
        catch (FileNotFoundException ex)
        {
            var panel = new Panel(
                "[red]Missing skill.json manifest in source directory[/]\n\n" +
                "[white]Remediation steps:[/]\n" +
                "1. Ensure the directory contains a valid [cyan]skill.json[/] file\n" +
                "2. Check the manifest structure: [cyan]{ \"id\", \"name\", \"version\", \"description\", \"entryPoint\" }[/]\n" +
                "3. See example manifest: [cyan]docs/guides/skill-manifest-schema.md[/]\n\n" +
                $"[dim]Error details: {Markup.Escape(ex.Message)}[/]"
            )
            {
                Header = new PanelHeader("[red]Invalid Skill Package[/]"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Red)
            };
            
            AnsiConsole.Write(panel);
            return 1;
        }
        catch (InvalidDataException ex)
        {
            var panel = new Panel(
                $"[red]Invalid manifest:[/] {Markup.Escape(ex.Message)}\n\n" +
                "[white]Remediation steps:[/]\n" +
                "1. Validate skill.json syntax: [cyan]cat skill.json | jq .[/]\n" +
                "2. Ensure required fields are present: id, name, version, description, entryPoint\n" +
                "3. Check field types (strings for most, bool for enabled, dict for dependencies)\n" +
                "4. See example: [cyan]docs/guides/skill-manifest-schema.md[/]"
            )
            {
                Header = new PanelHeader("[red]Manifest Validation Failed[/]"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Red)
            };
            
            AnsiConsole.Write(panel);
            return 1;
        }
        catch (InvalidOperationException ex)
        {
            var panel = new Panel(
                $"[red]Installation conflict:[/] {Markup.Escape(ex.Message)}\n\n" +
                "[white]Remediation steps:[/]\n" +
                "1. Uninstall existing skill: [cyan]mempalacenet skill uninstall <skill-id>[/]\n" +
                "2. Or use --force flag to overwrite (coming in Phase 3)\n" +
                "3. List installed skills: [cyan]mempalacenet skill list[/]"
            )
            {
                Header = new PanelHeader("[red]Skill Already Installed[/]"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Yellow)
            };
            
            AnsiConsole.Write(panel);
            return 1;
        }
        catch (Exception ex)
        {
            var panel = new Panel(
                $"[red]Unexpected installation error[/]\n\n" +
                $"[white]Error:[/] {Markup.Escape(ex.Message)}\n\n" +
                "[white]Remediation steps:[/]\n" +
                "1. Check file permissions on ~/.palace/skills/\n" +
                "2. Verify disk space is available\n" +
                "3. Try with elevated permissions if needed\n" +
                "4. Report this error: [cyan]https://github.com/elbruno/mempalacenet/issues[/]"
            )
            {
                Header = new PanelHeader("[red]Unexpected Error[/]"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Red)
            };
            
            AnsiConsole.Write(panel);
            return 2;
        }
    }
}
