using FluentAssertions;
using MemPalace.Benchmarks.Scoring;

namespace MemPalace.Tests.Benchmarks;

public sealed class MetricsTests
{
    [Fact]
    public void Recall_PerfectRecall_ReturnsOne()
    {
        // All relevant items retrieved
        var retrieved = new List<string> { "a", "b", "c" };
        var relevant = new List<string> { "a", "b" };

        var recall = Metrics.Recall(retrieved, relevant, k: 10);

        recall.Should().Be(1.0);
    }

    [Fact]
    public void Recall_PartialRecall_ReturnsCorrectFraction()
    {
        // Only 1 out of 2 relevant items retrieved
        var retrieved = new List<string> { "a", "x", "y" };
        var relevant = new List<string> { "a", "b" };

        var recall = Metrics.Recall(retrieved, relevant, k: 10);

        recall.Should().Be(0.5);
    }

    [Fact]
    public void Recall_NoRecall_ReturnsZero()
    {
        // None of the relevant items retrieved
        var retrieved = new List<string> { "x", "y", "z" };
        var relevant = new List<string> { "a", "b" };

        var recall = Metrics.Recall(retrieved, relevant, k: 10);

        recall.Should().Be(0.0);
    }

    [Fact]
    public void Recall_EmptyRelevant_ReturnsZero()
    {
        var retrieved = new List<string> { "a", "b" };
        var relevant = new List<string>();

        var recall = Metrics.Recall(retrieved, relevant, k: 10);

        recall.Should().Be(0.0);
    }

    [Fact]
    public void Precision_PerfectPrecision_ReturnsOne()
    {
        // All retrieved items are relevant
        var retrieved = new List<string> { "a", "b" };
        var relevant = new List<string> { "a", "b", "c" };

        var precision = Metrics.Precision(retrieved, relevant, k: 10);

        precision.Should().Be(1.0);
    }

    [Fact]
    public void Precision_PartialPrecision_ReturnsCorrectFraction()
    {
        // 2 out of 3 retrieved items are relevant
        var retrieved = new List<string> { "a", "b", "x" };
        var relevant = new List<string> { "a", "b" };

        var precision = Metrics.Precision(retrieved, relevant, k: 10);

        precision.Should().BeApproximately(0.6667, 0.001);
    }

    [Fact]
    public void Precision_NoPrecision_ReturnsZero()
    {
        // None of the retrieved items are relevant
        var retrieved = new List<string> { "x", "y", "z" };
        var relevant = new List<string> { "a", "b" };

        var precision = Metrics.Precision(retrieved, relevant, k: 10);

        precision.Should().Be(0.0);
    }

    [Fact]
    public void Precision_EmptyRetrieved_ReturnsZero()
    {
        var retrieved = new List<string>();
        var relevant = new List<string> { "a", "b" };

        var precision = Metrics.Precision(retrieved, relevant, k: 10);

        precision.Should().Be(0.0);
    }

    [Fact]
    public void F1_BalancedMetrics_ReturnsHarmonicMean()
    {
        var f1 = Metrics.F1(precision: 0.5, recall: 0.5);

        f1.Should().Be(0.5);
    }

    [Fact]
    public void F1_UnbalancedMetrics_ReturnsHarmonicMean()
    {
        var f1 = Metrics.F1(precision: 0.8, recall: 0.4);

        f1.Should().BeApproximately(0.5333, 0.001);
    }

    [Fact]
    public void F1_ZeroMetrics_ReturnsZero()
    {
        var f1 = Metrics.F1(precision: 0.0, recall: 0.0);

        f1.Should().Be(0.0);
    }

    [Fact]
    public void NdcgAtK_PerfectRanking_ReturnsOne()
    {
        // All relevant items at the top
        var retrieved = new List<string> { "a", "b", "x", "y" };
        var relevant = new List<string> { "a", "b" };

        var ndcg = Metrics.NdcgAtK(retrieved, relevant, k: 10);

        ndcg.Should().Be(1.0);
    }

    [Fact]
    public void NdcgAtK_ReversedRanking_ReturnsPenalizedScore()
    {
        // Relevant items at the end
        var retrieved = new List<string> { "x", "y", "a", "b" };
        var relevant = new List<string> { "a", "b" };

        var ndcg = Metrics.NdcgAtK(retrieved, relevant, k: 10);

        ndcg.Should().BeLessThan(1.0).And.BeGreaterThan(0.0);
    }

    [Fact]
    public void NdcgAtK_NoRelevantRetrieved_ReturnsZero()
    {
        var retrieved = new List<string> { "x", "y", "z" };
        var relevant = new List<string> { "a", "b" };

        var ndcg = Metrics.NdcgAtK(retrieved, relevant, k: 10);

        ndcg.Should().Be(0.0);
    }

    [Fact]
    public void NdcgAtK_EmptyRelevant_ReturnsZero()
    {
        var retrieved = new List<string> { "a", "b" };
        var relevant = new List<string>();

        var ndcg = Metrics.NdcgAtK(retrieved, relevant, k: 10);

        ndcg.Should().Be(0.0);
    }
}
