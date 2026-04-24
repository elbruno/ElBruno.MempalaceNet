using MemPalace.Agents;
using MemPalace.Agents.Registry;
using Microsoft.Extensions.AI;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace MemPalace.Cli.Commands.Agents;

internal sealed class AgentsRunSettings : CommandSettings
{
    [CommandArgument(0, "<agent-id>")]
    [Description("The ID of the agent to run")]
    public string AgentId { get; set; } = string.Empty;

    [CommandArgument(1, "<message>")]
    [Description("The message to send to the agent")]
    public string Message { get; set; } = string.Empty;
}

internal sealed class AgentsRunCommand : AsyncCommand<AgentsRunSettings>
{
    private readonly IAgentRegistry _agentRegistry;

    public AgentsRunCommand(IAgentRegistry agentRegistry)
    {
        _agentRegistry = agentRegistry;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, AgentsRunSettings settings)
    {
        try
        {
            var agent = _agentRegistry.Get(settings.AgentId);
            var ctx = new AgentContext(
                ConversationId: Guid.NewGuid().ToString(),
                History: Array.Empty<ChatMessage>(),
                Metadata: new Dictionary<string, object?>());

            AnsiConsole.MarkupLine($"[bold cyan]> {settings.Message}[/]");
            
            var response = await agent.InvokeAsync(settings.Message, ctx);

            AnsiConsole.MarkupLine($"[bold green]{agent.Descriptor.Name}:[/] {response.Content}");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[dim]Tokens: {response.Trace.InputTokens} in, {response.Trace.OutputTokens} out | Latency: {response.Trace.Latency.TotalMilliseconds:F0}ms[/]");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }
}
