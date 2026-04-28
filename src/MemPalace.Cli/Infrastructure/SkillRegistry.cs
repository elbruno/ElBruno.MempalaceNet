using System.Text.Json;
using MemPalace.Core.Model;

namespace MemPalace.Cli.Infrastructure;

/// <summary>
/// Manages skill discovery from local registry and built-in demo skills.
/// Phase 3: Local discovery only. Remote registry deferred to v1.0.
/// </summary>
public sealed class SkillRegistry
{
    private readonly string _registryPath;
    private readonly Lazy<List<SkillManifest>> _demoSkills;

    public SkillRegistry(string? registryPath = null)
    {
        _registryPath = registryPath ?? GetDefaultRegistryPath();
        _demoSkills = new Lazy<List<SkillManifest>>(LoadDemoSkills);
    }

    private static string GetDefaultRegistryPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".squad",
        "registry.json");

    /// <summary>
    /// Get all discoverable skills (installed + demo registry).
    /// </summary>
    public IReadOnlyList<SkillManifest> GetDiscoverableSkills()
    {
        var result = new Dictionary<string, SkillManifest>();

        // Add demo skills
        foreach (var skill in _demoSkills.Value)
        {
            if (skill.Discoverable)
            {
                result[skill.Id] = skill;
            }
        }

        return result.Values.ToList();
    }

    /// <summary>
    /// Search discoverable skills by tag.
    /// </summary>
    public IReadOnlyList<SkillManifest> SearchByTag(string tag)
    {
        var lowerTag = tag.ToLowerInvariant();
        return GetDiscoverableSkills()
            .Where(s => s.Tags.Any(t => t.Equals(lowerTag, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    /// <summary>
    /// Get skill info from registry (if not installed locally).
    /// </summary>
    public SkillManifest? GetRegistrySkill(string skillId)
    {
        return GetDiscoverableSkills()
            .FirstOrDefault(s => s.Id.Equals(skillId, StringComparison.OrdinalIgnoreCase));
    }

    private static List<SkillManifest> LoadDemoSkills()
    {
        return new List<SkillManifest>
        {
            new SkillManifest
            {
                Id = "rag-context-injector",
                Name = "RAG Context Injector",
                Version = "1.0.0",
                Description = "Semantic search + LLM context injection for RAG workflows",
                Author = "Bruno Capuano",
                Tags = new[] { "rag", "semantic-search", "llm", "context" },
                EntryPoint = "src/run.ps1",
                Enabled = true,
                Discoverable = true,
                Repository = "https://github.com/elbruno/mempalacenet-skills",
                License = "MIT",
                Dependencies = new Dictionary<string, string>
                {
                    { "mempalacenet", ">=0.7.0" },
                    { "Microsoft.Extensions.AI", ">=10.3.0" }
                }
            },
            new SkillManifest
            {
                Id = "agent-diary",
                Name = "Agent Diary Persistence",
                Version = "2.1.0",
                Description = "Persistent agent state across conversation sessions",
                Author = "Bruno Capuano",
                Tags = new[] { "agents", "persistence", "memory" },
                EntryPoint = "src/diary.ps1",
                Enabled = true,
                Discoverable = true,
                Repository = "https://github.com/elbruno/mempalacenet-skills",
                License = "MIT",
                Dependencies = new Dictionary<string, string>
                {
                    { "mempalacenet", ">=0.7.0" }
                }
            },
            new SkillManifest
            {
                Id = "kg-temporal-queries",
                Name = "Knowledge Graph Temporal Queries",
                Version = "0.8.0",
                Description = "Query knowledge graph relationships across time",
                Author = "Bruno Capuano",
                Tags = new[] { "knowledge-graph", "temporal", "queries" },
                EntryPoint = "src/temporal.ps1",
                Enabled = false,
                Discoverable = true,
                Repository = "https://github.com/elbruno/mempalacenet-skills",
                License = "MIT",
                Dependencies = new Dictionary<string, string>
                {
                    { "mempalacenet", ">=0.7.0" }
                }
            },
            new SkillManifest
            {
                Id = "hybrid-search-reranking",
                Name = "Hybrid Search + Reranking",
                Version = "1.5.0",
                Description = "LLM-based reranking for hybrid semantic/keyword search",
                Author = "Bruno Capuano",
                Tags = new[] { "search", "hybrid", "reranking", "llm" },
                EntryPoint = "src/rerank.ps1",
                Enabled = false,
                Discoverable = true,
                Repository = "https://github.com/elbruno/mempalacenet-skills",
                License = "MIT",
                Dependencies = new Dictionary<string, string>
                {
                    { "mempalacenet", ">=0.7.0" },
                    { "Microsoft.Extensions.AI", ">=10.3.0" }
                }
            }
        };
    }
}
