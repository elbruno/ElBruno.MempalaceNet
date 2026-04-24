using System.ComponentModel;
using MemPalace.KnowledgeGraph;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Commands.Kg;

internal sealed class KgAddSettings : CommandSettings
{
    [CommandArgument(0, "<subject>")]
    [Description("Subject entity (format: type:id)")]
    public string Subject { get; init; } = string.Empty;

    [CommandArgument(1, "<predicate>")]
    [Description("Relationship type")]
    public string Predicate { get; init; } = string.Empty;

    [CommandArgument(2, "<object>")]
    [Description("Object entity (format: type:id)")]
    public string Object { get; init; } = string.Empty;

    [CommandOption("--valid-from")]
    [Description("Validity start time (ISO 8601, defaults to now)")]
    public string? ValidFrom { get; init; }

    [CommandOption("--valid-to")]
    [Description("Validity end time (ISO 8601, defaults to indefinite)")]
    public string? ValidTo { get; init; }
}

internal sealed class KgAddCommand : AsyncCommand<KgAddSettings>
{
    private readonly IKnowledgeGraph _kg;

    public KgAddCommand(IKnowledgeGraph kg)
    {
        _kg = kg;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, KgAddSettings settings)
    {
        try
        {
            var subject = EntityRef.Parse(settings.Subject);
            var obj = EntityRef.Parse(settings.Object);
            
            var validFrom = string.IsNullOrEmpty(settings.ValidFrom) 
                ? DateTimeOffset.UtcNow 
                : DateTimeOffset.Parse(settings.ValidFrom);
            
            DateTimeOffset? validTo = string.IsNullOrEmpty(settings.ValidTo) 
                ? null 
                : DateTimeOffset.Parse(settings.ValidTo);
            
            var recordedAt = DateTimeOffset.UtcNow;
            
            var triple = new Triple(subject, settings.Predicate, obj, null);
            var temporal = new TemporalTriple(triple, validFrom, validTo, recordedAt);
            
            var id = await _kg.AddAsync(temporal);
            
            var panel = new Panel($"[green]✓[/] Added relationship (ID: {id})\n\n[blue]{subject}[/] --[green]{settings.Predicate}[/]--> [blue]{obj}[/]\nValid from: [yellow]{validFrom:yyyy-MM-dd HH:mm:ss}[/]\nValid to: [yellow]{(validTo.HasValue ? validTo.Value.ToString("yyyy-MM-dd HH:mm:ss") : "indefinite")}[/]")
            {
                Header = new PanelHeader("[bold green]mempalacenet kg add[/]"),
                Border = BoxBorder.Rounded
            };
            
            AnsiConsole.Write(panel);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }
}
