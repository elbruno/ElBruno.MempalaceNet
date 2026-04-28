using MemPalace.Cli.Infrastructure;
using Xunit;

namespace MemPalace.Tests.Cli.Skill;

public sealed class SkillRegistryTests
{
    [Fact]
    public void GetDiscoverableSkills_ReturnsBuiltInSkills()
    {
        var registry = new SkillRegistry();
        var skills = registry.GetDiscoverableSkills();

        Assert.NotEmpty(skills);
        Assert.True(skills.Count >= 4, "Should have at least 4 built-in demo skills");
        Assert.Contains(skills, s => s.Id == "rag-context-injector");
        Assert.Contains(skills, s => s.Id == "agent-diary");
        Assert.Contains(skills, s => s.Id == "kg-temporal-queries");
        Assert.Contains(skills, s => s.Id == "hybrid-search-reranking");
    }

    [Fact]
    public void GetDiscoverableSkills_SkillsAreDiscoverable()
    {
        var registry = new SkillRegistry();
        var skills = registry.GetDiscoverableSkills();

        Assert.All(skills, skill => Assert.True(skill.Discoverable, $"Skill '{skill.Id}' should be discoverable"));
    }

    [Fact]
    public void SearchByTag_ReturnsSkillsWithMatchingTag()
    {
        var registry = new SkillRegistry();
        var results = registry.SearchByTag("rag");

        Assert.NotEmpty(results);
        Assert.Contains(results, s => s.Id == "rag-context-injector");
        Assert.All(results, skill => Assert.Contains("rag", skill.Tags, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void SearchByTag_IsCaseInsensitive()
    {
        var registry = new SkillRegistry();
        var results1 = registry.SearchByTag("rag");
        var results2 = registry.SearchByTag("RAG");
        var results3 = registry.SearchByTag("Rag");

        Assert.Equal(results1.Count, results2.Count);
        Assert.Equal(results1.Count, results3.Count);
    }

    [Fact]
    public void SearchByTag_ReturnsEmptyForUnknownTag()
    {
        var registry = new SkillRegistry();
        var results = registry.SearchByTag("unknown-nonexistent-tag-xyz");

        Assert.Empty(results);
    }

    [Fact]
    public void GetRegistrySkill_ReturnsSkillByIdIfExists()
    {
        var registry = new SkillRegistry();
        var skill = registry.GetRegistrySkill("rag-context-injector");

        Assert.NotNull(skill);
        Assert.Equal("rag-context-injector", skill!.Id);
        Assert.Equal("RAG Context Injector", skill.Name);
    }

    [Fact]
    public void GetRegistrySkill_ReturnsNullForUnknownId()
    {
        var registry = new SkillRegistry();
        var skill = registry.GetRegistrySkill("nonexistent-skill");

        Assert.Null(skill);
    }

    [Fact]
    public void GetRegistrySkill_IsCaseInsensitive()
    {
        var registry = new SkillRegistry();
        var skill1 = registry.GetRegistrySkill("rag-context-injector");
        var skill2 = registry.GetRegistrySkill("RAG-CONTEXT-INJECTOR");
        var skill3 = registry.GetRegistrySkill("Rag-Context-Injector");

        Assert.NotNull(skill1);
        Assert.NotNull(skill2);
        Assert.NotNull(skill3);
        Assert.Equal(skill1!.Id, skill2!.Id);
        Assert.Equal(skill1.Id, skill3!.Id);
    }

    [Fact]
    public void BuiltInSkills_HaveRequiredMetadata()
    {
        var registry = new SkillRegistry();
        var skills = registry.GetDiscoverableSkills();

        foreach (var skill in skills)
        {
            Assert.NotEmpty(skill.Id);
            Assert.NotEmpty(skill.Name);
            Assert.NotEmpty(skill.Version);
            Assert.NotEmpty(skill.Description);
            Assert.NotEmpty(skill.EntryPoint);
            Assert.NotEmpty(skill.Author ?? "");
            Assert.NotNull(skill.Tags);
            Assert.NotNull(skill.Dependencies);
        }
    }

    [Fact]
    public void RagContextInjectorSkill_HasCorrectMetadata()
    {
        var registry = new SkillRegistry();
        var skill = registry.GetRegistrySkill("rag-context-injector");

        Assert.NotNull(skill);
        Assert.Equal("RAG Context Injector", skill!.Name);
        Assert.Equal("1.0.0", skill.Version);
        Assert.Contains("rag", skill.Tags, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("semantic-search", skill.Tags, StringComparer.OrdinalIgnoreCase);
        Assert.NotEmpty(skill.Dependencies);
        Assert.Contains("mempalacenet", skill.Dependencies.Keys);
    }

    [Fact]
    public void SearchByTag_ReturnsMultipleSkillsForCommonTag()
    {
        var registry = new SkillRegistry();
        var results = registry.SearchByTag("llm");

        Assert.True(results.Count >= 2, "Should find multiple skills with 'llm' tag");
    }
}
