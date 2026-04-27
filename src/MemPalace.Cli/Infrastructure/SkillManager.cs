using System.Text.Json;
using MemPalace.Core.Model;

namespace MemPalace.Cli.Infrastructure;

/// <summary>
/// Manages local skill installations and metadata.
/// Phase 1: Local filesystem operations only (no remote registry).
/// </summary>
public sealed class SkillManager
{
    private static readonly string SkillsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".palace",
        "skills");

    /// <summary>
    /// List all installed skills.
    /// </summary>
    public IReadOnlyList<SkillManifest> List()
    {
        if (!Directory.Exists(SkillsPath))
            return Array.Empty<SkillManifest>();

        var manifests = new List<SkillManifest>();
        
        foreach (var skillDir in Directory.GetDirectories(SkillsPath))
        {
            var manifestPath = Path.Combine(skillDir, "skill.json");
            if (File.Exists(manifestPath))
            {
                try
                {
                    var manifest = LoadManifest(manifestPath);
                    manifests.Add(manifest);
                }
                catch
                {
                    // Skip invalid manifests
                }
            }
        }
        
        return manifests;
    }

    /// <summary>
    /// Search installed skills by query (local filesystem only in Phase 1).
    /// </summary>
    public IReadOnlyList<SkillManifest> Search(string query)
    {
        var allSkills = List();
        var lowerQuery = query.ToLowerInvariant();
        
        return allSkills
            .Where(s => 
                s.Name.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
                s.Tags.Any(t => t.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    /// <summary>
    /// Get skill info by ID.
    /// </summary>
    public SkillManifest? GetInfo(string skillId)
    {
        var manifestPath = Path.Combine(SkillsPath, skillId, "skill.json");
        
        if (!File.Exists(manifestPath))
            return null;
        
        try
        {
            return LoadManifest(manifestPath);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Install a skill from a local path.
    /// Phase 1: Copies from local filesystem only (no remote registry).
    /// </summary>
    public async Task<bool> InstallAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        // Validate source exists
        if (!Directory.Exists(sourcePath) && !File.Exists(sourcePath))
            throw new DirectoryNotFoundException($"Source path not found: {sourcePath}");

        // Detect if source is a directory or archive
        var sourceDir = Directory.Exists(sourcePath) 
            ? sourcePath 
            : throw new NotSupportedException("Archive installation not implemented in Phase 1");

        // Validate manifest exists in source
        var sourceManifestPath = Path.Combine(sourceDir, "skill.json");
        if (!File.Exists(sourceManifestPath))
            throw new FileNotFoundException("skill.json not found in source directory");

        // Load and validate manifest
        var manifest = LoadManifest(sourceManifestPath);
        ValidateManifest(manifest);

        // Determine destination
        var destDir = Path.Combine(SkillsPath, manifest.Id);

        // Create skills directory if needed
        Directory.CreateDirectory(SkillsPath);

        // Check if already installed
        if (Directory.Exists(destDir))
            throw new InvalidOperationException($"Skill '{manifest.Id}' is already installed. Uninstall first.");

        // Copy skill directory
        await CopyDirectoryAsync(sourceDir, destDir, cancellationToken);

        return true;
    }

    /// <summary>
    /// Enable a skill (sets enabled flag in manifest).
    /// </summary>
    public async Task<bool> EnableAsync(string skillId, CancellationToken cancellationToken = default)
    {
        return await SetEnabledAsync(skillId, true, cancellationToken);
    }

    /// <summary>
    /// Disable a skill (sets enabled flag in manifest).
    /// </summary>
    public async Task<bool> DisableAsync(string skillId, CancellationToken cancellationToken = default)
    {
        return await SetEnabledAsync(skillId, false, cancellationToken);
    }

    /// <summary>
    /// Uninstall a skill (removes from ~/.palace/skills/).
    /// </summary>
    public bool Uninstall(string skillId)
    {
        var skillDir = Path.Combine(SkillsPath, skillId);
        
        if (!Directory.Exists(skillDir))
            return false;

        Directory.Delete(skillDir, recursive: true);
        return true;
    }

    // Private helpers

    private static SkillManifest LoadManifest(string path)
    {
        var json = File.ReadAllText(path);
        var manifest = JsonSerializer.Deserialize<SkillManifest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        return manifest ?? throw new InvalidDataException("Invalid skill manifest");
    }

    private static void ValidateManifest(SkillManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest.Id))
            throw new InvalidDataException("Skill ID is required");
        
        if (string.IsNullOrWhiteSpace(manifest.Name))
            throw new InvalidDataException("Skill name is required");
        
        if (string.IsNullOrWhiteSpace(manifest.Version))
            throw new InvalidDataException("Skill version is required");
        
        if (string.IsNullOrWhiteSpace(manifest.Description))
            throw new InvalidDataException("Skill description is required");
        
        if (string.IsNullOrWhiteSpace(manifest.EntryPoint))
            throw new InvalidDataException("Skill entry point is required");
    }

    private async Task<bool> SetEnabledAsync(string skillId, bool enabled, CancellationToken cancellationToken)
    {
        var manifestPath = Path.Combine(SkillsPath, skillId, "skill.json");
        
        if (!File.Exists(manifestPath))
            return false;

        var manifest = LoadManifest(manifestPath);
        
        // Update enabled flag
        var updatedManifest = manifest with { Enabled = enabled };
        
        // Serialize back to file
        var json = JsonSerializer.Serialize(updatedManifest, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        await File.WriteAllTextAsync(manifestPath, json, cancellationToken);
        
        return true;
    }

    private static async Task CopyDirectoryAsync(string sourceDir, string destDir, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destDir);

        // Copy all files
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(destDir, fileName);
            await Task.Run(() => File.Copy(file, destFile, overwrite: false), cancellationToken);
        }

        // Recursively copy subdirectories
        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(subDir);
            var destSubDir = Path.Combine(destDir, dirName);
            await CopyDirectoryAsync(subDir, destSubDir, cancellationToken);
        }
    }
}
