using Microsoft.Extensions.Configuration;

namespace CustomSkill;

/// <summary>
/// Service interface for custom skill operations.
/// Demonstrates how to structure reusable skill logic.
/// </summary>
public interface ICustomSkillService
{
    /// <summary>
    /// Performs a custom operation (e.g., semantic search with enrichment).
    /// </summary>
    Task<CustomSkillResult> ExecuteAsync(string query, string? wing = null, int limit = 10);
}

/// <summary>
/// Implementation of custom skill service.
/// Replace with your actual business logic.
/// </summary>
public class CustomSkillService : ICustomSkillService
{
    private readonly IConfiguration _config;

    public CustomSkillService(IConfiguration config)
    {
        _config = config;
    }

    public async Task<CustomSkillResult> ExecuteAsync(string query, string? wing = null, int limit = 10)
    {
        // Get configuration
        var enabled = _config.GetValue("custom-skill:enabled", true);
        var defaultWing = _config.GetValue("custom-skill:default-wing", "default");
        var resultLimit = Math.Min(limit, _config.GetValue("custom-skill:result-limit", 10));

        if (!enabled)
            throw new InvalidOperationException("Custom skill is disabled.");

        // Mock implementation - replace with actual Palace integration
        await Task.Delay(100); // Simulate async operation

        var targetWing = wing ?? defaultWing;

        return new CustomSkillResult
        {
            Query = query,
            Wing = targetWing,
            Timestamp = DateTime.UtcNow,
            Items = new[]
            {
                new CustomSkillItem
                {
                    Score = 0.95f,
                    Content = $"Mock result 1 for query: {query}",
                    Metadata = new Dictionary<string, object>
                    {
                        { "source", "template" },
                        { "wing", targetWing }
                    }
                },
                new CustomSkillItem
                {
                    Score = 0.87f,
                    Content = $"Mock result 2 for query: {query}",
                    Metadata = new Dictionary<string, object>
                    {
                        { "source", "template" },
                        { "wing", targetWing }
                    }
                }
            }.Take(resultLimit).ToArray()
        };
    }
}

/// <summary>
/// Result from custom skill execution.
/// </summary>
public class CustomSkillResult
{
    public required string Query { get; set; }
    public required string Wing { get; set; }
    public required DateTime Timestamp { get; set; }
    public required CustomSkillItem[] Items { get; set; }
}

/// <summary>
/// Individual item in skill results.
/// </summary>
public class CustomSkillItem
{
    public required float Score { get; set; }
    public required string Content { get; set; }
    public required Dictionary<string, object> Metadata { get; set; }
}
