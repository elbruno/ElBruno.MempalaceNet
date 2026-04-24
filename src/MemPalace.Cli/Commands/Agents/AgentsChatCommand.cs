using MemPalace.Agents;
using MemPalace.Agents.Registry;
using Microsoft.Extensions.AI;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace MemPalace.Cli.Commands.Agents;

internal sealed class AgentsChatSettings : CommandSettings
{
    [CommandArgument(0, "<agent-id>")]
    [Description("The ID of the agent to chat with")]
    public string AgentId { get; set; } = string.Empty;
}

internal sealed class AgentsChatCommand : AsyncCommand<AgentsChatSettings>
{
    private readonly IAgentRegistry _agentRegistry;

    public AgentsChatCommand(IAgentRegistry agentRegistry)
    {
        _agentRegistry = agentRegistry;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, AgentsChatSettings settings)
    {
        try
        {
            var agent = _agentRegistry.Get(settings.AgentId);
            var history = new List<ChatMessage>();
            var conversationId = Guid.NewGuid().ToString();

            AnsiConsole.MarkupLine($"[bold]Starting chat with {agent.Descriptor.Name}[/]");
            AnsiConsole.MarkupLine("[dim]Type 'exit' to quit[/]");
            AnsiConsole.WriteLine();

            while (true)
            {
                var userMessage = AnsiConsole.Prompt(
                    new TextPrompt<string>("[bold cyan]You:[/]")
                        .AllowEmpty());

                if (string.IsNullOrWhiteSpace(userMessage) || userMessage.ToLower() == "exit")
                {
                    break;
                }

                var ctx = new AgentContext(conversationId, history, new Dictionary<string, object?>());
                var response = await agent.InvokeAsync(userMessage, ctx);

                history.AddRange(response.NewMessages);

                AnsiConsole.MarkupLine($"[bold green]{agent.Descriptor.Name}:[/] {response.Content}");
                AnsiConsole.WriteLine();
            }

            AnsiConsole.MarkupLine("[dim]Chat ended.[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }
}
