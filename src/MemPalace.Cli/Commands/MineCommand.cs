using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Commands;

internal sealed class MineSettings : CommandSettings
{
    [CommandArgument(0, "<path>")]
    [Description("Path to mine for memories")]
    public string Path { get; init; } = string.Empty;

    [CommandOption("--mode")]
    [Description("Mining mode: files or convos")]
    [DefaultValue("files")]
    public string Mode { get; init; } = "files";

    [CommandOption("--wing")]
    [Description("Target wing for mined content")]
    public string? Wing { get; init; }
}

internal sealed class MineCommand : AsyncCommand<MineSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, MineSettings settings)
    {
        var panel = new Panel($"[yellow]TODO(phase4): implementation pending[/]\n\nWill mine: [blue]{settings.Path}[/]\nMode: [blue]{settings.Mode}[/]\nWing: [blue]{settings.Wing ?? "(auto-detect)"}[/]")
        {
            Header = new PanelHeader("[bold green]mempalace mine[/]"),
            Border = BoxBorder.Rounded
        };
        
        AnsiConsole.Write(panel);
        
        // Stub progress bar placeholder
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[green]Mining memories[/]");
                task.MaxValue = 100;
                
                for (int i = 0; i <= 100; i += 20)
                {
                    await Task.Delay(50);
                    task.Value = i;
                }
            });
        
        AnsiConsole.MarkupLine("[green]✓[/] Mining complete (stub)");
        return 0;
    }
}
