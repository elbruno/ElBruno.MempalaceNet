using FluentAssertions;
using MemPalace.Core.Model;
using System.Text.Json;

namespace MemPalace.Tests.Cli.Skill;

public sealed class SkillManifestTests
{
    [Fact]
    public void SkillManifest_Deserializes_FromValidJson()
    {
        // Arrange
        var json = @"{
            ""id"": ""test-skill"",
            ""name"": ""Test Skill"",
            ""version"": ""1.0.0"",
            ""description"": ""A test skill"",
            ""author"": ""Test Author"",
            ""tags"": [""test"", ""example""],
            ""dependencies"": {
                ""other-skill"": ""^2.0.0""
            },
            ""entryPoint"": ""main.js"",
            ""enabled"": true,
            ""repository"": ""https://github.com/test/test"",
            ""license"": ""MIT""
        }";

        // Act
        var manifest = JsonSerializer.Deserialize<SkillManifest>(json);

        // Assert
        manifest.Should().NotBeNull();
        manifest!.Id.Should().Be("test-skill");
        manifest.Name.Should().Be("Test Skill");
        manifest.Version.Should().Be("1.0.0");
        manifest.Description.Should().Be("A test skill");
        manifest.Author.Should().Be("Test Author");
        manifest.Tags.Should().BeEquivalentTo(new[] { "test", "example" });
        manifest.Dependencies.Should().ContainKey("other-skill");
        manifest.EntryPoint.Should().Be("main.js");
        manifest.Enabled.Should().BeTrue();
        manifest.Repository.Should().Be("https://github.com/test/test");
        manifest.License.Should().Be("MIT");
    }

    [Fact]
    public void SkillManifest_Serializes_ToJson()
    {
        // Arrange
        var manifest = new SkillManifest
        {
            Id = "test-skill",
            Name = "Test Skill",
            Version = "1.0.0",
            Description = "A test skill",
            Author = "Test Author",
            Tags = new[] { "test", "example" },
            Dependencies = new Dictionary<string, string> { { "other-skill", "^2.0.0" } },
            EntryPoint = "main.js",
            Enabled = true,
            Repository = "https://github.com/test/test",
            License = "MIT"
        };

        // Act
        var json = JsonSerializer.Serialize(manifest);
        var deserialized = JsonSerializer.Deserialize<SkillManifest>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(manifest.Id);
        deserialized.Name.Should().Be(manifest.Name);
        deserialized.Version.Should().Be(manifest.Version);
    }

    [Fact]
    public void SkillManifest_HasDefaultValues_ForOptionalFields()
    {
        // Arrange
        var json = @"{
            ""id"": ""minimal-skill"",
            ""name"": ""Minimal Skill"",
            ""version"": ""1.0.0"",
            ""description"": ""Minimal manifest"",
            ""entryPoint"": ""index.js""
        }";

        // Act
        var manifest = JsonSerializer.Deserialize<SkillManifest>(json);

        // Assert
        manifest.Should().NotBeNull();
        manifest!.Tags.Should().BeEmpty();
        manifest.Dependencies.Should().BeEmpty();
        manifest.Enabled.Should().BeTrue(); // Default is true
        manifest.Author.Should().BeNull();
        manifest.Repository.Should().BeNull();
        manifest.License.Should().BeNull();
    }

    [Fact]
    public void SkillManifest_WithRecord_SupportsImmutability()
    {
        // Arrange
        var manifest = new SkillManifest
        {
            Id = "test-skill",
            Name = "Test Skill",
            Version = "1.0.0",
            Description = "A test skill",
            EntryPoint = "main.js"
        };

        // Act - Use 'with' expression to create modified copy
        var updated = manifest with { Enabled = false };

        // Assert
        manifest.Enabled.Should().BeTrue(); // Original unchanged
        updated.Enabled.Should().BeFalse(); // New instance modified
        updated.Id.Should().Be(manifest.Id); // Other properties copied
    }
}
