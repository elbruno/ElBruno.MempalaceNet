using System.ComponentModel;
using MemPalace.KnowledgeGraph;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Commands.Kg;

internal sealed class KgQuerySettings : CommandSettings
{
    [CommandArgument(0, "<pattern>")]
    [Description("Query pattern: 'subject predicate object' - use '?' for wildcards")]
    public string Pattern { get; init; } = string.Empty;

    [CommandOption("--at")]
    [Description("Query as of specific time (ISO 8601)")]
    public string? At { get; init; }
}

internal sealed class KgQueryCommand : AsyncCommand<KgQuerySettings>
{
    private readonly IKnowledgeGraph _kg;

    public KgQueryCommand(IKnowledgeGraph kg)
    {
        _kg = kg;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, KgQuerySettings settings)
    {
        try
        {
            var parts = settings.Pattern.Split(' ', 3, StringSplitOptions.TrimEntries);
            if (parts.Length != 3)
            {
                AnsiConsole.MarkupLine("[red]Pattern must have 3 parts: subject predicate object (use ? for wildcards)[/]");
                return 1;
            }

            EntityRef? subject = parts[0] == "?" ? null : EntityRef.Parse(parts[0]);
            string? predicate = parts[1] == "?" ? null : parts[1];
            EntityRef? obj = parts[2] == "?" ? null : EntityRef.Parse(parts[2]);

            var pattern = new TriplePattern(subject, predicate, obj);

            DateTimeOffset? at = string.IsNullOrEmpty(settings.At) 
                ? null 
                : DateTimeOffset.Parse(settings.At);

            var results = await _kg.QueryAsync(pattern, at);

            var panel = new Panel($"Pattern: [cyan]{settings.Pattern}[/]\nAt time: [yellow]{(at.HasValue ? at.Value.ToString("yyyy-MM-dd HH:mm:ss") : "current")}[/]\nResults: [green]{results.Count}[/]")
            {
                Header = new PanelHeader("[bold green]mempalacenet kg query[/]"),
                Border = BoxBorder.Rounded
            };

            AnsiConsole.Write(panel);

            if (results.Count > 0)
            {
                var table = new Table();
                table.AddColumn("Subject");
                table.AddColumn("Predicate");
                table.AddColumn("Object");
                table.AddColumn("Valid From");
                table.AddColumn("Valid To");

                foreach (var result in results)
                {
                    table.AddRow(
                        result.Triple.Subject.ToString(),
                        result.Triple.Predicate,
                        result.Triple.Object.ToString(),
                        result.ValidFrom.ToString("yyyy-MM-dd HH:mm:ss"),
                        result.ValidTo?.ToString("yyyy-MM-dd HH:mm:ss") ?? "∞"
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
