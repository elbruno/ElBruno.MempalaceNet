using System.Text.Json.Serialization;

namespace MemPalace.Core.Model;

/// <summary>
/// Skill manifest schema (skill.json).
/// </summary>
public sealed record SkillManifest
{
    /// <summary>
    /// Unique skill identifier (kebab-case).
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }
    
    /// <summary>
    /// Skill display name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    
    /// <summary>
    /// Semantic version (e.g., "1.0.0").
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; init; }
    
    /// <summary>
    /// Short skill description.
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; init; }
    
    /// <summary>
    /// Author name or organization.
    /// </summary>
    [JsonPropertyName("author")]
    public string? Author { get; init; }
    
    /// <summary>
    /// Skill tags for discovery/filtering.
    /// </summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// Skill dependencies (other skill IDs).
    /// </summary>
    [JsonPropertyName("dependencies")]
    public IReadOnlyDictionary<string, string> Dependencies { get; init; } = new Dictionary<string, string>();
    
    /// <summary>
    /// Entry point script path (relative to skill root).
    /// </summary>
    [JsonPropertyName("entryPoint")]
    public required string EntryPoint { get; init; }
    
    /// <summary>
    /// Skill enabled status (toggleable by user).
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;
    
    /// <summary>
    /// Repository URL (optional).
    /// </summary>
    [JsonPropertyName("repository")]
    public string? Repository { get; init; }
    
    /// <summary>
    /// License identifier (SPDX format, e.g., "MIT").
    /// </summary>
    [JsonPropertyName("license")]
    public string? License { get; init; }
}
