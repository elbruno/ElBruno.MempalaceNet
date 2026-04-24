using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Commands;

internal sealed class InitSettings : CommandSettings
{
    [CommandArgument(0, "<path>")]
    [Description("Path where the palace will be initialized")]
    public string Path { get; init; } = string.Empty;

    [CommandOption("--name")]
    [Description("Optional name for the palace")]
    public string? Name { get; init; }
}

internal sealed class InitCommand : AsyncCommand<InitSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, InitSettings settings)
    {
        var panel = new Panel($"[yellow]TODO(phase4): implementation pending[/]\n\nWill initialize palace at: [blue]{settings.Path}[/]\nName: [blue]{settings.Name ?? "(default)"}[/]")
        {
            Header = new PanelHeader("[bold green]mempalacenet init[/]"),
            Border = BoxBorder.Rounded
        };
        
        AnsiConsole.Write(panel);
        await Task.CompletedTask;
        return 0;
    }
}
