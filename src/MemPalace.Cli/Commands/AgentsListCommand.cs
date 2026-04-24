using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Commands;

internal sealed class AgentsListCommand : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var panel = new Panel("[yellow]TODO(phase8): implementation pending[/]")
        {
            Header = new PanelHeader("[bold green]mempalace agents list[/]"),
            Border = BoxBorder.Rounded
        };
        
        AnsiConsole.Write(panel);
        
        // Stub agents table
        var table = new Table();
        table.AddColumn("Agent");
        table.AddColumn("Wing");
        table.AddColumn("Last Active");
        table.AddColumn("Memory Count");
        
        table.AddRow("Tyrell", "code/tyrell", "2 hours ago", "342");
        table.AddRow("Roy", "ai/roy", "1 day ago", "189");
        table.AddRow("Rachael", "cli/rachael", "5 minutes ago", "67");
        table.AddRow("Deckard", "architecture/deckard", "3 days ago", "521");
        
        AnsiConsole.Write(table);
        
        await Task.CompletedTask;
        return 0;
    }
}
