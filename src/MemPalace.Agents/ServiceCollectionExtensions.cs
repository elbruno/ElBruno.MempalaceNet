using Microsoft.Extensions.DependencyInjection;
using MemPalace.Agents.Diary;
using MemPalace.Agents.Registry;
using MemPalace.Agents.Runtime;

namespace MemPalace.Agents;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMemPalaceAgents(
        this IServiceCollection services,
        Action<MemPalaceAgentsOptions>? configure = null)
    {
        var options = new MemPalaceAgentsOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IAgentDiary, BackedByPalaceDiary>();
        services.AddSingleton<IMemPalaceAgentBuilder, MemPalaceAgentBuilder>();
        services.AddSingleton<IAgentRegistry>(sp =>
        {
            var chatClient = sp.GetService<Microsoft.Extensions.AI.IChatClient>();
            var builder = sp.GetRequiredService<IMemPalaceAgentBuilder>();
            
            if (chatClient == null)
            {
                // Return empty registry if no chat client is configured
                return new EmptyAgentRegistry();
            }

            return new YamlAgentRegistry(options.AgentsPath, chatClient, builder);
        });

        return services;
    }
    
    // Empty registry for when IChatClient is not available
    private class EmptyAgentRegistry : IAgentRegistry
    {
        public IReadOnlyList<AgentDescriptor> List() => Array.Empty<AgentDescriptor>();
        public IMemPalaceAgent Get(string id) => throw new InvalidOperationException("No chat client configured. Call AddMemPalaceAi() before AddMemPalaceAgents().");
    }
}

public class MemPalaceAgentsOptions
{
    public string AgentsPath { get; set; } = Path.Combine(
        Directory.GetCurrentDirectory(),
        ".mempalace",
        "agents");
}
