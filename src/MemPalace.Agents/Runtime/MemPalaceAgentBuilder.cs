using Microsoft.Extensions.AI;
using MemPalace.Agents.Diary;
using MemPalace.Search;
using MemPalace.KnowledgeGraph;
using MemPalace.Core.Backends;

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
                async (string query, string collection = "default", int topK = 5) =>
                {
                    var opts = new SearchOptions(TopK: topK, Wing: null, Rerank: false);
                    var results = await _searchService.SearchAsync(query, collection, opts);
                    return string.Join("\n", results.Select(r => $"[{r.Score:F2}] {r.Document}"));
                },
                "palace_search",
                "Search for memories in the palace");
            tools.Add(searchFunc);
        }

        if (_knowledgeGraph != null)
        {
            var kgFunc = AIFunctionFactory.Create(
                async (string subject, string predicate, string obj) =>
                {
                    var pattern = new TriplePattern(
                        string.IsNullOrEmpty(subject) ? null : EntityRef.Parse(subject),
                        string.IsNullOrEmpty(predicate) ? null : predicate,
                        string.IsNullOrEmpty(obj) ? null : EntityRef.Parse(obj));
                    var results = await _knowledgeGraph.QueryAsync(pattern);
                    return string.Join("\n", results.Select(t => 
                        $"{t.Triple.Subject} {t.Triple.Predicate} {t.Triple.Object}"));
                },
                "kg_query",
                "Query the knowledge graph for relationships");
            tools.Add(kgFunc);
        }

        var clientWithTools = tools.Count > 0
            ? chatClient.AsBuilder().UseFunctionInvocation().Build()
            : chatClient;

        return new MemPalaceAgent(descriptor, clientWithTools, _diary);
    }
}
