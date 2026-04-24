using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Commands.Kg;

internal sealed class KgAddSettings : CommandSettings
{
    [CommandArgument(0, "<subject>")]
    [Description("Subject entity")]
    public string Subject { get; init; } = string.Empty;

    [CommandArgument(1, "<predicate>")]
    [Description("Relationship type")]
    public string Predicate { get; init; } = string.Empty;

    [CommandArgument(2, "<object>")]
    [Description("Object entity")]
    public string Object { get; init; } = string.Empty;

    [CommandOption("--valid-from")]
    [Description("Validity start time (ISO 8601)")]
    public string? ValidFrom { get; init; }

    [CommandOption("--valid-to")]
    [Description("Validity end time (ISO 8601)")]
    public string? ValidTo { get; init; }
}

internal sealed class KgAddCommand : AsyncCommand<KgAddSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, KgAddSettings settings)
    {
        var panel = new Panel($"[yellow]TODO(phase6): implementation pending[/]\n\nWill add relationship:\n[blue]{settings.Subject}[/] --[green]{settings.Predicate}[/]--> [blue]{settings.Object}[/]\nValid from: [blue]{settings.ValidFrom ?? "(now)"}[/]\nValid to: [blue]{settings.ValidTo ?? "(indefinite)"}[/]")
        {
            Header = new PanelHeader("[bold green]mempalace kg add[/]"),
            Border = BoxBorder.Rounded
        };
        
        AnsiConsole.Write(panel);
        await Task.CompletedTask;
        return 0;
    }
}
