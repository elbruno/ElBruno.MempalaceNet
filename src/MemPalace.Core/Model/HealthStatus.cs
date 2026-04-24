namespace MemPalace.Core.Model;

/// <summary>
/// Represents the health status of a backend.
/// </summary>
public sealed record HealthStatus(bool Ok, string Detail = "")
{
    public static HealthStatus Healthy(string detail = "") => new(true, detail);
    public static HealthStatus Unhealthy(string detail = "") => new(false, detail);
}
