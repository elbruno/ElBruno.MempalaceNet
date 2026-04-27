using FluentAssertions;
using MemPalace.Cli.Infrastructure;
using System.Text.Json;

namespace MemPalace.Tests.Cli.Skill;

public sealed class SkillManagerTests : IDisposable
{
    private readonly string _tempSkillsPath;
    private readonly string _originalHome;

    public SkillManagerTests()
    {
        // Create temp directory for tests
        _tempSkillsPath = Path.Combine(Path.GetTempPath(), "mempalace-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempSkillsPath);

        // Override home directory for testing
        _originalHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        
        // Note: We can't actually change the home directory, so tests will use a helper
        // that overrides the SkillManager path behavior
    }

    public void Dispose()
    {
        // Clean up temp directory
        if (Directory.Exists(_tempSkillsPath))
        {
            Directory.Delete(_tempSkillsPath, recursive: true);
        }
    }

    [Fact]
    public void List_ReturnsEmpty_WhenNoSkillsInstalled()
    {
        // Arrange
        var manager = new SkillManager();

        // Act
        var skills = manager.List();

        // Assert
        skills.Should().BeEmpty();
    }

    [Fact]
    public async Task InstallAsync_CopiesSkillDirectory_WhenValidManifest()
    {
        // Arrange
        var manager = new SkillManager();
        var sourceDir = CreateTestSkill("test-skill", "1.0.0", "Test Skill");

        try
        {
            // Act
            var result = await manager.InstallAsync(sourceDir);

            // Assert
            result.Should().BeTrue();
            var skills = manager.List();
            skills.Should().ContainSingle()
                .Which.Id.Should().Be("test-skill");
        }
        finally
        {
            // Cleanup
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Fact]
    public async Task InstallAsync_ThrowsFileNotFoundException_WhenManifestMissing()
    {
        // Arrange
        var manager = new SkillManager();
        var sourceDir = Path.Combine(_tempSkillsPath, "no-manifest");
        Directory.CreateDirectory(sourceDir);

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => manager.InstallAsync(sourceDir));
        
        Directory.Delete(sourceDir, recursive: true);
    }

    [Fact]
    public async Task InstallAsync_ThrowsInvalidDataException_WhenManifestInvalid()
    {
        // Arrange
        var manager = new SkillManager();
        var sourceDir = Path.Combine(_tempSkillsPath, "invalid-skill");
        Directory.CreateDirectory(sourceDir);
        
        // Create invalid manifest (missing required fields)
        var manifestPath = Path.Combine(sourceDir, "skill.json");
        File.WriteAllText(manifestPath, "{}");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDataException>(
            () => manager.InstallAsync(sourceDir));
        
        Directory.Delete(sourceDir, recursive: true);
    }

    [Fact]
    public async Task Search_ReturnsMatchingSkills_ByName()
    {
        // Arrange
        var manager = new SkillManager();
        var skill1 = CreateTestSkill("embedding-skill", "1.0.0", "Embedding helper");
        var skill2 = CreateTestSkill("rag-skill", "1.0.0", "RAG implementation");

        try
        {
            await manager.InstallAsync(skill1);
            await manager.InstallAsync(skill2);

            // Act
            var results = manager.Search("embedding");

            // Assert
            results.Should().ContainSingle()
                .Which.Id.Should().Be("embedding-skill");
        }
        finally
        {
            Directory.Delete(skill1, recursive: true);
            Directory.Delete(skill2, recursive: true);
        }
    }

    [Fact]
    public void GetInfo_ReturnsNull_WhenSkillNotFound()
    {
        // Arrange
        var manager = new SkillManager();

        // Act
        var skill = manager.GetInfo("nonexistent-skill");

        // Assert
        skill.Should().BeNull();
    }

    [Fact]
    public async Task EnableAsync_UpdatesManifest_WhenSkillExists()
    {
        // Arrange
        var manager = new SkillManager();
        var sourceDir = CreateTestSkill("test-skill", "1.0.0", "Test Skill", enabled: false);

        try
        {
            await manager.InstallAsync(sourceDir);

            // Act
            var result = await manager.EnableAsync("test-skill");

            // Assert
            result.Should().BeTrue();
            var skill = manager.GetInfo("test-skill");
            skill.Should().NotBeNull();
            skill!.Enabled.Should().BeTrue();
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Fact]
    public async Task DisableAsync_UpdatesManifest_WhenSkillExists()
    {
        // Arrange
        var manager = new SkillManager();
        var sourceDir = CreateTestSkill("test-skill", "1.0.0", "Test Skill", enabled: true);

        try
        {
            await manager.InstallAsync(sourceDir);

            // Act
            var result = await manager.DisableAsync("test-skill");

            // Assert
            result.Should().BeTrue();
            var skill = manager.GetInfo("test-skill");
            skill.Should().NotBeNull();
            skill!.Enabled.Should().BeFalse();
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Fact]
    public async Task Uninstall_RemovesSkillDirectory_WhenSkillExists()
    {
        // Arrange
        var manager = new SkillManager();
        var sourceDir = CreateTestSkill("test-skill", "1.0.0", "Test Skill");

        try
        {
            await manager.InstallAsync(sourceDir);

            // Act
            var result = manager.Uninstall("test-skill");

            // Assert
            result.Should().BeTrue();
            manager.GetInfo("test-skill").Should().BeNull();
        }
        finally
        {
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Fact]
    public void Uninstall_ReturnsFalse_WhenSkillNotFound()
    {
        // Arrange
        var manager = new SkillManager();

        // Act
        var result = manager.Uninstall("nonexistent-skill");

        // Assert
        result.Should().BeFalse();
    }

    // Helper methods

    private string CreateTestSkill(
        string id, 
        string version, 
        string description, 
        bool enabled = true,
        string[]? tags = null)
    {
        var skillDir = Path.Combine(_tempSkillsPath, id);
        Directory.CreateDirectory(skillDir);

        var manifest = new
        {
            id,
            name = id.Replace("-", " ").ToTitleCase(),
            version,
            description,
            author = "Test Author",
            tags = tags ?? Array.Empty<string>(),
            dependencies = new Dictionary<string, string>(),
            entryPoint = "main.js",
            enabled,
            repository = "https://github.com/test/test",
            license = "MIT"
        };

        var manifestPath = Path.Combine(skillDir, "skill.json");
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            WriteIndented = true
        }));

        // Create a dummy entry point file
        File.WriteAllText(Path.Combine(skillDir, "main.js"), "// Entry point");

        return skillDir;
    }
}

// Helper extension for title case
internal static class StringExtensions
{
    public static string ToTitleCase(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        var words = str.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + (words[i].Length > 1 ? words[i][1..] : "");
            }
        }
        return string.Join(" ", words);
    }
}
