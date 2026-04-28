using MemPalace.Ai.Embedding;
using MemPalace.Backends.Sqlite;
using MemPalace.Benchmarks.Core;
using MemPalace.Core.Backends;
using Microsoft.Extensions.DependencyInjection;

namespace MemPalace.Benchmarks.Commands;

internal static class BenchmarkServiceBuilder
{
    public static IServiceProvider Build(string embedderKind, string? model, string? endpoint)
    {
        var services = new ServiceCollection();
        var normalizedKind = string.IsNullOrWhiteSpace(embedderKind)
            ? "deterministic"
            : embedderKind.Trim().ToLowerInvariant();

        switch (normalizedKind)
        {
            case "deterministic":
                services.AddSingleton<IEmbedder>(new DeterministicEmbedder(384));
                break;

            case "local":
                services.AddMemPalaceAi(options =>
                {
                    options.Type = EmbedderType.Local;
                    if (!string.IsNullOrWhiteSpace(model))
                    {
                        options.Model = model;
                    }
                });
                break;

            case "ollama":
                // Ollama is not supported in stable releases, but keep the case for benchmarks
                throw new InvalidOperationException(
                    "Ollama embedder is not available in stable releases. Use 'local' or 'deterministic' instead.");

            default:
                throw new InvalidOperationException(
                    $"Unknown embedder '{embedderKind}'. Supported values: deterministic, local, ollama.");
        }

        services.AddSingleton<IBackend>(_ => new SqliteBackend());
        return services.BuildServiceProvider();
    }
}
