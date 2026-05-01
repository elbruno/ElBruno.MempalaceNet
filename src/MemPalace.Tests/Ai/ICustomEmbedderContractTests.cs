using FluentAssertions;
using MemPalace.Core.Backends;

namespace MemPalace.Tests.Ai;

/// <summary>
/// Contract tests for ICustomEmbedder implementations.
/// Validates that custom embedders adhere to the interface contract.
/// </summary>
public sealed class ICustomEmbedderContractTests
{
    [Fact]
    public void CustomEmbedder_ModelIdentity_IsNotNullOrEmpty()
    {
        // Arrange
        var embedder = new ValidCustomEmbedder();

        // Act & Assert
        embedder.ModelIdentity.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CustomEmbedder_Dimensions_IsPositive()
    {
        // Arrange
        var embedder = new ValidCustomEmbedder();

        // Act & Assert
        embedder.Dimensions.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CustomEmbedder_EmbedAsync_EmptyList_ReturnsEmpty()
    {
        // Arrange
        var embedder = new ValidCustomEmbedder();

        // Act
        var result = await embedder.EmbedAsync(Array.Empty<string>());

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CustomEmbedder_EmbedAsync_ReturnsDimensionsMatchingProperty()
    {
        // Arrange
        var embedder = new ValidCustomEmbedder();

        // Act
        var result = await embedder.EmbedAsync(new[] { "test" });

        // Assert
        result.Should().HaveCount(1);
        result[0].Length.Should().Be(embedder.Dimensions);
    }

    [Fact]
    public async Task CustomEmbedder_EmbedAsync_ThreadSafe()
    {
        // Arrange
        var embedder = new ValidCustomEmbedder();
        const int concurrentCalls = 10;

        // Act - make concurrent calls
        var tasks = Enumerable.Range(0, concurrentCalls)
            .Select(i => embedder.EmbedAsync(new[] { $"text-{i}" }).AsTask())
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert - all calls should succeed
        results.Should().HaveCount(concurrentCalls);
        results.Should().AllSatisfy(result => result.Should().HaveCount(1));
    }

    [Fact]
    public async Task CustomEmbedder_EmbedAsync_Idempotent()
    {
        // Arrange
        var embedder = new ValidCustomEmbedder();
        const string text = "consistent text";

        // Act - call twice with same input
        var result1 = await embedder.EmbedAsync(new[] { text });
        var result2 = await embedder.EmbedAsync(new[] { text });

        // Assert - should return same embedding
        result1[0].Span.ToArray().Should().Equal(result2[0].Span.ToArray());
    }

    [Fact]
    public async Task CustomEmbedder_EmbedAsync_NormalizedVectors()
    {
        // Arrange
        var embedder = new ValidCustomEmbedder();

        // Act
        var result = await embedder.EmbedAsync(new[] { "test normalization" });

        // Assert - vectors should be normalized (magnitude ≈ 1.0)
        var embedding = result[0].Span.ToArray();
        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
        magnitude.Should().BeApproximately(1.0f, 0.01f);
    }

    [Fact]
    public async Task CustomEmbedder_EmbedAsync_CancellationToken_Respected()
    {
        // Arrange
        var embedder = new ValidCustomEmbedder();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await embedder.EmbedAsync(new[] { "test" }, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // Valid custom embedder for testing
    private sealed class ValidCustomEmbedder : ICustomEmbedder
    {
        public string ProviderName => "valid";
        public string ModelIdentity => "test-valid-embedder-v1";
        public int Dimensions => 128;
        public IReadOnlyDictionary<string, object> Metadata => new Dictionary<string, object>
        {
            { "provider", "valid" },
            { "model", "valid-embedder-v1" }
        };

        public async ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
            IReadOnlyList<string> texts,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (texts == null || texts.Count == 0)
            {
                return Array.Empty<ReadOnlyMemory<float>>();
            }

            var results = new List<ReadOnlyMemory<float>>();
            foreach (var text in texts)
            {
                var embedding = ComputeEmbedding(text);
                results.Add(embedding);
            }

            return await ValueTask.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(results);
        }

        private ReadOnlyMemory<float> ComputeEmbedding(string text)
        {
            var embedding = new float[Dimensions];

            // Deterministic: same text always produces same embedding
            var hash = text.GetHashCode();
            var random = new Random(hash);

            // Generate random values in [-1, 1]
            for (int i = 0; i < Dimensions; i++)
            {
                embedding[i] = (float)(random.NextDouble() * 2.0 - 1.0);
            }

            // Normalize to unit vector
            var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
            if (magnitude > 0)
            {
                for (int i = 0; i < Dimensions; i++)
                {
                    embedding[i] /= magnitude;
                }
            }

            return embedding;
        }
    }
}
