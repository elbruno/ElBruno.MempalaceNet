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
        
        // Register IAgentDiary - requires IBackend, IEmbedder, and ISearchService
        services.AddSingleton<IAgentDiary>(sp =>
        {
            var backend = sp.GetService<MemPalace.Core.Backends.IBackend>();
            var embedder = sp.GetService<MemPalace.Core.Backends.IEmbedder>();
            // Note: ISearchService requires IBackend and IEmbedder, so we check those directly
            
            if (backend == null || embedder == null)
            {
                // Return empty diary if backend/embedder not configured
                return new InMemoryAgentDiary();
            }

            var searchService = sp.GetRequiredService<MemPalace.Search.ISearchService>();
            return new BackedByPalaceDiary(backend, embedder, searchService);
        });
        
        // Register IMemPalaceAgentBuilder with optional dependencies
        services.AddSingleton<IMemPalaceAgentBuilder>(sp =>
        {
            var diary = sp.GetService<IAgentDiary>();
            var backend = sp.GetService<MemPalace.Core.Backends.IBackend>();
            var knowledgeGraph = sp.GetService<MemPalace.KnowledgeGraph.IKnowledgeGraph>();
            
            // Only get searchService if backend is available (searchService requires backend)
            MemPalace.Search.ISearchService? searchService = null;
            if (backend != null)
            {
                searchService = sp.GetService<MemPalace.Search.ISearchService>();
            }
            
            return new MemPalaceAgentBuilder(diary, searchService, knowledgeGraph, backend);
        });
        
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
    
    // In-memory diary for when backend/embedder/search not available
    private class InMemoryAgentDiary : IAgentDiary
    {
        private readonly List<DiaryEntry> _entries = new();

        public Task AppendAsync(string agentId, DiaryEntry entry, CancellationToken ct = default)
        {
            _entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<DiaryEntry>> RecentAsync(string agentId, int take = 50, CancellationToken ct = default)
        {
            var results = _entries
                .Where(e => e.AgentId == agentId)
                .OrderByDescending(e => e.At)
                .Take(take)
                .ToList();
            return Task.FromResult<IReadOnlyList<DiaryEntry>>(results);
        }

        public Task<IReadOnlyList<DiaryEntry>> SearchAsync(string agentId, string query, int topK = 10, CancellationToken ct = default)
        {
            var results = _entries
                .Where(e => e.AgentId == agentId)
                .OrderByDescending(e => e.At)
                .Take(topK)
                .ToList();
            return Task.FromResult<IReadOnlyList<DiaryEntry>>(results);
        }
    }
}

public class MemPalaceAgentsOptions
{
    public string AgentsPath { get; set; } = Path.Combine(
        Directory.GetCurrentDirectory(),
        ".mempalace",
        "agents");
}
