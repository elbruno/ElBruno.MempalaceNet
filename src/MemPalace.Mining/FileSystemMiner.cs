using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.FileSystemGlobbing;

namespace MemPalace.Mining;

/// <summary>
/// Mines files from a directory, respecting .gitignore patterns and chunking large files.
/// </summary>
public sealed class FileSystemMiner : IMiner
{
    private const int DefaultChunkSize = 2000;
    private const int DefaultOverlap = 200;
    
    private static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".so", ".dylib", ".bin", ".dat", ".db", ".sqlite", ".jpg", ".jpeg", 
        ".png", ".gif", ".bmp", ".ico", ".pdf", ".zip", ".tar", ".gz", ".7z", ".rar",
        ".mp3", ".mp4", ".avi", ".mkv", ".mov", ".wav", ".flac", ".woff", ".woff2", ".ttf", ".eot"
    };

    public string Name => "filesystem";

    public async IAsyncEnumerable<MinedItem> MineAsync(
        MinerContext ctx,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var chunkSize = ParseOption(ctx.Options, "chunk_size", DefaultChunkSize);
        var overlap = ParseOption(ctx.Options, "overlap", DefaultOverlap);

        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        
        // Default include all text files
        matcher.AddInclude("**/*");
        
        // Parse custom includes/excludes
        if (ctx.Options.TryGetValue("include", out var includePattern) && includePattern != null)
        {
            matcher.AddInclude(includePattern);
        }
        
        if (ctx.Options.TryGetValue("exclude", out var excludePattern) && excludePattern != null)
        {
            matcher.AddExclude(excludePattern);
        }

        // Load .gitignore patterns
        var gitignorePath = Path.Combine(ctx.SourcePath, ".gitignore");
        if (File.Exists(gitignorePath))
        {
            var patterns = await File.ReadAllLinesAsync(gitignorePath, ct);
            foreach (var pattern in patterns)
            {
                var trimmed = pattern.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                    continue;
                    
                // .gitignore patterns are exclusions
                matcher.AddExclude(trimmed);
            }
        }

        var result = matcher.Execute(
            new Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper(
                new DirectoryInfo(ctx.SourcePath)));

        foreach (var file in result.Files)
        {
            ct.ThrowIfCancellationRequested();
            
            var fullPath = Path.Combine(ctx.SourcePath, file.Path);
            var extension = Path.GetExtension(fullPath);
            
            // Skip binary files
            if (BinaryExtensions.Contains(extension))
                continue;

            FileInfo fileInfo;
            try
            {
                fileInfo = new FileInfo(fullPath);
            }
            catch
            {
                continue; // Skip inaccessible files
            }

            if (!fileInfo.Exists)
                continue;

            string content;
            try
            {
                content = await File.ReadAllTextAsync(fullPath, ct);
            }
            catch
            {
                continue; // Skip unreadable files
            }

            // Check for null bytes (binary indicator)
            if (content.Contains('\0'))
                continue;

            // Compute SHA256 prefix for de-dupe
            var sha256Prefix = ComputeSha256Prefix(content);

            // Chunk if needed
            if (content.Length <= chunkSize)
            {
                var metadata = new Dictionary<string, object?>
                {
                    ["path"] = file.Path,
                    ["ext"] = extension,
                    ["size"] = fileInfo.Length,
                    ["mtime"] = fileInfo.LastWriteTimeUtc.ToString("O"),
                    ["sha256_8"] = sha256Prefix
                };

                yield return new MinedItem(
                    Id: $"{sha256Prefix}:{file.Path}",
                    Content: content,
                    Metadata: metadata);
            }
            else
            {
                // Chunk large files
                var chunkIndex = 0;
                for (var i = 0; i < content.Length; i += chunkSize - overlap)
                {
                    var remaining = content.Length - i;
                    var length = Math.Min(chunkSize, remaining);
                    var chunk = content.Substring(i, length);

                    var metadata = new Dictionary<string, object?>
                    {
                        ["path"] = file.Path,
                        ["ext"] = extension,
                        ["size"] = fileInfo.Length,
                        ["mtime"] = fileInfo.LastWriteTimeUtc.ToString("O"),
                        ["sha256_8"] = sha256Prefix,
                        ["chunk_index"] = chunkIndex,
                        ["chunk_start"] = i,
                        ["chunk_end"] = i + length
                    };

                    yield return new MinedItem(
                        Id: $"{sha256Prefix}:{file.Path}:chunk{chunkIndex}",
                        Content: chunk,
                        Metadata: metadata);

                    chunkIndex++;
                }
            }
        }
    }

    private static string ComputeSha256Prefix(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash)[..8].ToLowerInvariant();
    }

    private static int ParseOption(IReadOnlyDictionary<string, string?> options, string key, int defaultValue)
    {
        if (options.TryGetValue(key, out var value) && int.TryParse(value, out var parsed))
            return parsed;
        return defaultValue;
    }
}
