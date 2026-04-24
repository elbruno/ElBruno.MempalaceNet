using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using MemPalace.Agents.Diary;
using MemPalace.Search;
using MemPalace.KnowledgeGraph;
using MemPalace.Core.Backends;
using System.ComponentModel;

namespace MemPalace.Agents.Runtime;

public class MemPalaceAgentBuilder : IMemPalaceAgentBuilder
{
    private readonly IAgentDiary? _diary;
    private readonly ISearchService? _searchService;
    private readonly IKnowledgeGraph? _knowledgeGraph;
    private readonly IBackend? _backend;

    public MemPalaceAgentBuilder(
        IAgentDiary? diary = null,
        ISearchService? searchService = null,
        IKnowledgeGraph? knowledgeGraph = null,
        IBackend? backend = null)
    {
        _diary = diary;
        _searchService = searchService;
        _knowledgeGraph = knowledgeGraph;
        _backend = backend;
    }

    public IMemPalaceAgent Build(AgentDescriptor descriptor, IChatClient chatClient)
    {
        var tools = new List<AITool>();

        if (_searchService != null)
        {
            var searchFunc = AIFunctionFactory.Create(
                [Description("Search for memories in the palace matching the query")]
                async ([Description("The search query text")] string query,
                       [Description("The collection/wing to search in")] string collection = "default",
                       [Description("Number of results to return")] int topK = 5,
                       CancellationToken ct = default) =>
                {
                    var opts = new SearchOptions(TopK: topK, Wing: null, Rerank: false);
                    var results = await _searchService.SearchAsync(query, collection, opts, ct);
                    return string.Join("\n", results.Select(r => $"[{r.Score:F2}] {r.Document}"));
                },
                "palace_search");
            tools.Add(searchFunc);
        }

        if (_knowledgeGraph != null)
        {
            var kgFunc = AIFunctionFactory.Create(
                [Description("Query the knowledge graph for entity relationships (triples). Use null for wildcards.")]
                async ([Description("Subject entity (e.g., 'agent:roy') or null for any")] string? subject,
                       [Description("Predicate/relationship (e.g., 'worked-on') or null for any")] string? predicate,
                       [Description("Object entity (e.g., 'project:MemPalace.Mcp') or null for any")] string? @object,
                       CancellationToken ct = default) =>
                {
                    var pattern = new TriplePattern(
                        !string.IsNullOrEmpty(subject) ? EntityRef.Parse(subject) : null,
                        !string.IsNullOrEmpty(predicate) ? predicate : null,
                        !string.IsNullOrEmpty(@object) ? EntityRef.Parse(@object) : null);
                    var results = await _knowledgeGraph.QueryAsync(pattern, ct: ct);
                    return string.Join("\n", results.Select(t =>
                        $"{t.Triple.Subject} {t.Triple.Predicate} {t.Triple.Object}"));
                },
                "kg_query");
            tools.Add(kgFunc);
        }

        // Compose instructions from persona + instructions
        var instructions = !string.IsNullOrEmpty(descriptor.Persona)
            ? $"{descriptor.Persona}\n\n{descriptor.Instructions ?? string.Empty}".Trim()
            : descriptor.Instructions ?? "You are a helpful assistant";

        // Create the ChatClientAgent with tools
        // Note: ChatClientAgent doesn't have a description parameter, only name and instructions
        var agent = new ChatClientAgent(
            chatClient,
            name: descriptor.Name,
            description: null,  // AgentDescriptor doesn't have Description field
            instructions: instructions,
            tools: tools.Count > 0 ? tools : null);

        return new MemPalaceAgent(descriptor, agent, _diary);
    }
}
