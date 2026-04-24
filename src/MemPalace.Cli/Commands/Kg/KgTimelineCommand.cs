using System.ComponentModel;
using MemPalace.KnowledgeGraph;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Commands.Kg;

internal sealed class KgTimelineSettings : CommandSettings
{
    [CommandArgument(0, "<entity>")]
    [Description("Entity to view timeline for (format: type:id)")]
    public string Entity { get; init; } = string.Empty;

    [CommandOption("--from")]
    [Description("Start time (ISO 8601)")]
    public string? From { get; init; }

    [CommandOption("--to")]
    [Description("End time (ISO 8601)")]
    public string? To { get; init; }
}

internal sealed class KgTimelineCommand : AsyncCommand<KgTimelineSettings>
{
    private readonly IKnowledgeGraph _kg;

    public KgTimelineCommand(IKnowledgeGraph kg)
    {
        _kg = kg;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, KgTimelineSettings settings)
    {
        try
        {
            var entity = EntityRef.Parse(settings.Entity);

            DateTimeOffset? from = string.IsNullOrEmpty(settings.From) 
                ? null 
                : DateTimeOffset.Parse(settings.From);

            DateTimeOffset? to = string.IsNullOrEmpty(settings.To) 
                ? null 
                : DateTimeOffset.Parse(settings.To);

            var timeline = await _kg.TimelineAsync(entity, from, to);

            var panel = new Panel($"Entity: [cyan]{entity}[/]\nFrom: [yellow]{(from.HasValue ? from.Value.ToString("yyyy-MM-dd HH:mm:ss") : "earliest")}[/]\nTo: [yellow]{(to.HasValue ? to.Value.ToString("yyyy-MM-dd HH:mm:ss") : "latest")}[/]\nEvents: [green]{timeline.Count}[/]")
            {
                Header = new PanelHeader("[bold green]mempalacenet kg timeline[/]"),
                Border = BoxBorder.Rounded
            };

            AnsiConsole.Write(panel);

            if (timeline.Count > 0)
            {
                var table = new Table();
                table.AddColumn("Time");
                table.AddColumn("Direction");
                table.AddColumn("Predicate");
                table.AddColumn("Other Entity");

                foreach (var evt in timeline)
                {
                    var directionSymbol = evt.Direction == "outgoing" ? "→" : "←";
                    var directionColor = evt.Direction == "outgoing" ? "green" : "blue";
                    
                    table.AddRow(
                        evt.At.ToString("yyyy-MM-dd HH:mm:ss"),
                        $"[{directionColor}]{directionSymbol} {evt.Direction}[/]",
                        evt.Predicate,
                        evt.Other.ToString()
                    );
                }

                AnsiConsole.Write(table);
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }
}
