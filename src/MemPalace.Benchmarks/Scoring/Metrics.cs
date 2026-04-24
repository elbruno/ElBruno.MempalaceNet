namespace MemPalace.Benchmarks.Scoring;

/// <summary>
/// Metrics computation for benchmark evaluation.
/// </summary>
public static class Metrics
{
    /// <summary>
    /// Computes recall at k: fraction of relevant items in the top-k retrieved.
    /// </summary>
    public static double Recall(IReadOnlyList<string> retrieved, IReadOnlyList<string> relevant, int k)
    {
        if (relevant.Count == 0)
            return 0.0;

        var topK = retrieved.Take(k).ToHashSet();
        var found = relevant.Count(id => topK.Contains(id));
        return (double)found / relevant.Count;
    }

    /// <summary>
    /// Computes precision at k: fraction of retrieved items that are relevant.
    /// </summary>
    public static double Precision(IReadOnlyList<string> retrieved, IReadOnlyList<string> relevant, int k)
    {
        var topK = retrieved.Take(k).ToList();
        if (topK.Count == 0)
            return 0.0;

        var relevantSet = relevant.ToHashSet();
        var found = topK.Count(id => relevantSet.Contains(id));
        return (double)found / topK.Count;
    }

    /// <summary>
    /// Computes F1 score from precision and recall.
    /// </summary>
    public static double F1(double precision, double recall)
    {
        if (precision + recall == 0.0)
            return 0.0;
        return 2.0 * precision * recall / (precision + recall);
    }

    /// <summary>
    /// Computes normalized discounted cumulative gain at k (NDCG@k).
    /// Assumes binary relevance (1 if relevant, 0 otherwise).
    /// </summary>
    public static double NdcgAtK(IReadOnlyList<string> retrieved, IReadOnlyList<string> relevant, int k)
    {
        if (relevant.Count == 0)
            return 0.0;

        var relevantSet = relevant.ToHashSet();
        var topK = retrieved.Take(k).ToList();

        // DCG: sum of (relevance / log2(position + 1))
        var dcg = 0.0;
        for (var i = 0; i < topK.Count; i++)
        {
            var rel = relevantSet.Contains(topK[i]) ? 1.0 : 0.0;
            dcg += rel / Math.Log2(i + 2);  // position is 1-indexed, so i+2
        }

        // IDCG: ideal DCG if all relevant items were at the top
        var idealK = Math.Min(k, relevant.Count);
        var idcg = 0.0;
        for (var i = 0; i < idealK; i++)
        {
            idcg += 1.0 / Math.Log2(i + 2);
        }

        return idcg == 0.0 ? 0.0 : dcg / idcg;
    }
}
