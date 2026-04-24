using MemPalace.Agents;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MemPalace.Tests.Agents;

public sealed class AgentDescriptorParseTests
{
    [Fact]
    public void ParseYaml_ValidAgent_ReturnsDescriptor()
    {
        var yaml = @"
id: test-agent
name: Test Agent
persona: You are a test agent.
instructions: Follow test instructions.
wing: testing
";
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var agentYaml = deserializer.Deserialize<AgentYaml>(yaml);
        var descriptor = new AgentDescriptor(
            agentYaml.Id,
            agentYaml.Name,
            agentYaml.Persona,
            agentYaml.Instructions,
            agentYaml.Wing);

        Assert.Equal("test-agent", descriptor.Id);
        Assert.Equal("Test Agent", descriptor.Name);
        Assert.Equal("You are a test agent.", descriptor.Persona);
        Assert.Equal("Follow test instructions.", descriptor.Instructions);
        Assert.Equal("testing", descriptor.Wing);
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
