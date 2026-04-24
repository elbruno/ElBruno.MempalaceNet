using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using MemPalace.Agents.Runtime;
using Microsoft.Extensions.AI;

namespace MemPalace.Agents.Registry;

public class YamlAgentRegistry : IAgentRegistry
{
    private readonly string _agentsPath;
    private readonly IChatClient _chatClient;
    private readonly IMemPalaceAgentBuilder _agentBuilder;
    private readonly Dictionary<string, IMemPalaceAgent> _agentCache = new();

    public YamlAgentRegistry(
        string agentsPath,
        IChatClient chatClient,
        IMemPalaceAgentBuilder agentBuilder)
    {
        _agentsPath = agentsPath;
        _chatClient = chatClient;
        _agentBuilder = agentBuilder;
    }

    public IReadOnlyList<AgentDescriptor> List()
    {
        if (!Directory.Exists(_agentsPath))
        {
            return Array.Empty<AgentDescriptor>();
        }

        var yamlFiles = Directory.GetFiles(_agentsPath, "*.yaml", SearchOption.TopDirectoryOnly);
        var descriptors = new List<AgentDescriptor>();

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        foreach (var file in yamlFiles)
        {
            try
            {
                var yaml = File.ReadAllText(file);
                var agentYaml = deserializer.Deserialize<AgentYaml>(yaml);
                descriptors.Add(new AgentDescriptor(
                    agentYaml.Id,
                    agentYaml.Name,
                    agentYaml.Persona,
                    agentYaml.Instructions,
                    agentYaml.Wing));
            }
            catch
            {
                // Skip invalid files
            }
        }

        return descriptors;
    }

    public IMemPalaceAgent Get(string id)
    {
        if (_agentCache.TryGetValue(id, out var cached))
        {
            return cached;
        }

        var descriptor = List().FirstOrDefault(d => d.Id == id);
        if (descriptor == null)
        {
            throw new InvalidOperationException($"Agent '{id}' not found");
        }

        var agent = _agentBuilder.Build(descriptor, _chatClient);
        _agentCache[id] = agent;
        return agent;
    }

    private class AgentYaml
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Persona { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public string? Wing { get; set; }
    }
}
