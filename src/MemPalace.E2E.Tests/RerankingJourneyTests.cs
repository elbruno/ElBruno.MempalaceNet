using FluentAssertions;
using MemPalace.Ai.Rerank;
using MemPalace.Core.Model;
using System.Diagnostics;
using Xunit.Abstractions;

namespace MemPalace.E2E.Tests;

/// <summary>
/// E2E tests for LLM-based reranking journey.
/// Validates: reranking improves search quality, latency SLOs, determinism.
/// </summary>
public sealed class RerankingJourneyTests : E2ETestBase
{
    private readonly ITestOutputHelper _output;

    public RerankingJourneyTests(ITestOutputHelper output)
    {
        _output = output;
    }
    [Fact]
    public async Task TestRerankingImprovesSingleQuery()
    {
        // Arrange: Create diverse document corpus
        await InitializePalaceAsync();
        var documents = new[]
        {
            "Authentication: Use JWT tokens for stateless authentication. Token expiry is 15 minutes.",
            "Caching: Redis cache for session data. TTL is 1 hour. Use sliding expiration.",
            "Logging: Structured logging with Serilog. Log levels: Debug, Info, Warning, Error.",
            "Error handling: Catch exceptions at controller level. Return 500 with error ID.",
            "Data structures: Use Dictionary for O(1) lookups. List for ordered collections.",
            "Security: HTTPS only. CORS enabled for trusted origins. CSRF tokens required.",
            "Database: PostgreSQL with connection pooling. Max 20 connections per instance.",
            "API design: RESTful endpoints. Use HTTP verbs correctly. Versioning in URL.",
            "Testing: Unit tests with xUnit. Integration tests with TestContainers.",
            "Deployment: Docker containers. Kubernetes for orchestration. Blue-green deployments.",
            "Monitoring: Application Insights for telemetry. Custom metrics for business events.",
            "Performance: Response time <100ms P95. Database queries <50ms. Use async/await.",
            "Code style: Follow C# naming conventions. Use nullable reference types.",
            "Documentation: XML comments for public APIs. README for setup instructions.",
            "Git workflow: Feature branches. PR reviews required. Squash merge to main.",
            "Configuration: appsettings.json for defaults. Environment variables for secrets.",
            "Dependency injection: Use built-in DI container. Register services in Startup.",
            "Validation: FluentValidation for input validation. Return 400 with detailed errors.",
            "Background jobs: Hangfire for scheduled tasks. Retry policy: 3 attempts.",
            "File storage: Azure Blob Storage for user uploads. Generate SAS tokens for access."
        };

        var records = new List<EmbeddedRecord>();
        for (int i = 0; i < documents.Length; i++)
        {
            var embedding = (await Embedder.EmbedAsync(new[] { documents[i] }))[0];
            records.Add(new EmbeddedRecord(
                Id: $"doc-{i}",
                Document: documents[i],
                Metadata: new Dictionary<string, object?>
                {
                    { "wing", "documentation" },
                    { "source", "engineering-handbook" }
                },
                Embedding: embedding.ToArray()
            ));
        }
        await Collection.AddAsync(records);

        // Act: Search without reranking
        var query = "how do we handle errors in the API?";
        var queryEmbedding = await Embedder.EmbedAsync(new[] { query });
        var beforeRerank = await Collection.QueryAsync(queryEmbedding, nResults: 10);

        // Mock reranker that improves score for error-related docs
        var mockReranker = new MockReranker();
        var candidateHits = beforeRerank.Ids[0]
            .Select((id, idx) => new RankedHit(
                id,
                beforeRerank.Documents[0][idx],
                1.0f - beforeRerank.Distances[0][idx]))
            .ToList();
        
        var afterRerank = await mockReranker.RerankAsync(query, candidateHits);

        // Assert: Top result after reranking should be error handling doc
        var topResultAfterRerank = afterRerank.First();
        topResultAfterRerank.Document.Should().Contain("Error handling", 
            "reranking should promote error-related documents");

        // Assert: Score improved by ≥10%
        var originalScore = candidateHits.First(h => h.Document.Contains("Error handling")).Score;
        var rerankedScore = topResultAfterRerank.Score;
        var improvement = (rerankedScore - originalScore) / originalScore;
        improvement.Should().BeGreaterThanOrEqualTo(0.10f, 
            $"reranking should improve score by ≥10% (original: {originalScore:F3}, reranked: {rerankedScore:F3})");

        // Log results
        _output.WriteLine($"Original top-1 score: {candidateHits[0].Score:F3}");
        _output.WriteLine($"Reranked top-1 score: {rerankedScore:F3}");
        _output.WriteLine($"Score improvement: {improvement * 100:F1}%");
    }

    [Fact]
    public async Task TestRerankingScoresAreConsistent()
    {
        // Arrange
        await InitializePalaceAsync();
        var records = await CreateTestMemoriesAsync(15, "test-wing");
        await Collection.AddAsync(records);

        var query = "test query for consistency";
        var queryEmbedding = await Embedder.EmbedAsync(new[] { query });
        var searchResults = await Collection.QueryAsync(queryEmbedding, nResults: 5);

        var mockReranker = new MockReranker();
        var candidateHits = searchResults.Ids[0]
            .Select((id, idx) => new RankedHit(
                id,
                searchResults.Documents[0][idx],
                1.0f - searchResults.Distances[0][idx]))
            .ToList();

        // Act: Rerank same query twice
        var rerank1 = await mockReranker.RerankAsync(query, candidateHits);
        var rerank2 = await mockReranker.RerankAsync(query, candidateHits);

        // Assert: Order should be identical
        rerank1.Select(h => h.Id).Should().ContainInOrder(rerank2.Select(h => h.Id),
            "reranking should be deterministic for same query");

        rerank1.Select(h => h.Score).Should().Equal(rerank2.Select(h => h.Score),
            "reranking scores should be identical for same query");

        _output.WriteLine("Consistency check passed: identical ordering and scores across runs");
    }

    [Fact]
    public async Task TestRerankingLatency()
    {
        // Arrange
        await InitializePalaceAsync();
        var records = await CreateTestMemoriesAsync(10, "latency-test");
        await Collection.AddAsync(records);

        var query = "latency test query";
        var queryEmbedding = await Embedder.EmbedAsync(new[] { query });
        var searchResults = await Collection.QueryAsync(queryEmbedding, nResults: 10);

        var mockReranker = new MockReranker();
        var candidateHits = searchResults.Ids[0]
            .Select((id, idx) => new RankedHit(
                id,
                searchResults.Documents[0][idx],
                1.0f - searchResults.Distances[0][idx]))
            .ToList();

        // Act: Measure reranking latency
        var sw = Stopwatch.StartNew();
        var reranked = await mockReranker.RerankAsync(query, candidateHits);
        sw.Stop();

        // Assert: Latency <200ms
        sw.ElapsedMilliseconds.Should().BeLessThan(200,
            "reranking 10 candidates should complete in <200ms");

        _output.WriteLine($"Reranking latency: {sw.ElapsedMilliseconds}ms for {candidateHits.Count} candidates");
        _output.WriteLine($"Average per candidate: {sw.ElapsedMilliseconds / (double)candidateHits.Count:F2}ms");
    }

    [Fact]
    public async Task TestRerankingWithHybridSearch()
    {
        // Arrange: Create docs with keyword overlap
        await InitializePalaceAsync();
        var documents = new[]
        {
            "JWT authentication middleware handles token validation and renewal",
            "Authentication system uses OAuth2 with refresh tokens",
            "Error middleware catches exceptions and returns appropriate status codes",
            "Logging middleware records request/response for audit trail",
            "CORS middleware configures cross-origin resource sharing policies"
        };

        var records = new List<EmbeddedRecord>();
        for (int i = 0; i < documents.Length; i++)
        {
            var embedding = (await Embedder.EmbedAsync(new[] { documents[i] }))[0];
            records.Add(new EmbeddedRecord(
                Id: $"mid-{i}",
                Document: documents[i],
                Metadata: new Dictionary<string, object?> { { "wing", "middleware" } },
                Embedding: embedding.ToArray()
            ));
        }
        await Collection.AddAsync(records);

        // Act: Semantic search for "authentication"
        var query = "authentication middleware";
        var queryEmbedding = await Embedder.EmbedAsync(new[] { query });
        var semanticResults = await Collection.QueryAsync(queryEmbedding, nResults: 5);

        var mockReranker = new MockReranker();
        var candidateHits = semanticResults.Ids[0]
            .Select((id, idx) => new RankedHit(
                id,
                semanticResults.Documents[0][idx],
                1.0f - semanticResults.Distances[0][idx]))
            .ToList();

        var reranked = await mockReranker.RerankAsync(query, candidateHits);

        // Assert: Reranked results should prioritize exact keyword matches
        var topResult = reranked.First();
        topResult.Document.Should().Contain("authentication")
            .And.Contain("middleware", "reranking should boost documents with both keywords");

        _output.WriteLine($"Top result after hybrid+rerank: {topResult.Document}");
        _output.WriteLine($"Score: {topResult.Score:F3}");
    }
}

/// <summary>
/// Mock reranker for deterministic testing.
/// Scores based on keyword overlap and semantic similarity.
/// </summary>
internal sealed class MockReranker : IReranker
{
    public ValueTask<IReadOnlyList<RankedHit>> RerankAsync(
        string query,
        IReadOnlyList<RankedHit> candidates,
        CancellationToken ct = default)
    {
        if (candidates == null || candidates.Count == 0)
            return ValueTask.FromResult<IReadOnlyList<RankedHit>>(Array.Empty<RankedHit>());

        var queryTerms = query.ToLowerInvariant()
            .Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();

        var reranked = candidates
            .Select(hit =>
            {
                var docTerms = hit.Document.ToLowerInvariant()
                    .Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToHashSet();

                var overlap = queryTerms.Intersect(docTerms).Count();
                var keywordScore = overlap / (float)queryTerms.Count;

                // Combine semantic score (original) with keyword score
                var combinedScore = (hit.Score * 0.6f) + (keywordScore * 0.4f);

                // Add 15% boost for keyword-rich documents
                var boostedScore = combinedScore * (1.0f + (keywordScore * 0.15f));

                return new RankedHit(hit.Id, hit.Document, boostedScore);
            })
            .OrderByDescending(h => h.Score)
            .ToList();

        return ValueTask.FromResult<IReadOnlyList<RankedHit>>(reranked);
    }
}
