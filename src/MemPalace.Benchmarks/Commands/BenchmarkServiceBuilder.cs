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
                    options.Provider = "Local";
                    if (!string.IsNullOrWhiteSpace(model))
                    {
                        options.Model = model;
                    }
                });
                break;

            case "ollama":
                services.AddMemPalaceAi(options =>
                {
                    options.Provider = "Ollama";
                    options.Model = string.IsNullOrWhiteSpace(model) ? "nomic-embed-text" : model;
                    if (!string.IsNullOrWhiteSpace(endpoint))
                    {
                        options.Endpoint = endpoint;
                    }
                });
                break;

            default:
                throw new InvalidOperationException(
                    $"Unknown embedder '{embedderKind}'. Supported values: deterministic, local, ollama.");
        }

        services.AddSingleton<IBackend>(_ => new SqliteBackend());
        return services.BuildServiceProvider();
    }
}
