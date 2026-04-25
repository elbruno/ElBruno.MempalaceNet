using MemPalace.Agents.Registry;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Commands.Agents;

internal sealed class AgentsListSettings : CommandSettings
{
}

internal sealed class AgentsListCommand : AsyncCommand<AgentsListSettings>
{
    private readonly IAgentRegistry _agentRegistry;

    public AgentsListCommand(IAgentRegistry agentRegistry)
    {
        _agentRegistry = agentRegistry;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, AgentsListSettings settings)
    {
        var agents = _agentRegistry.List();

        if (agents.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No agents found. Create agent YAML files in .mempalace/agents/[/]");
            return 0;
        }

        var table = new Table();
        table.AddColumn("ID");
        table.AddColumn("Name");
        table.AddColumn("Wing");
        
        foreach (var agent in agents)
        {
            table.AddRow(
                agent.Id,
                agent.Name,
                agent.Wing ?? "-");
        }
        
        AnsiConsole.Write(table);
        
        await Task.CompletedTask;
        return 0;
    }
}
