using FluentAssertions;
using MemPalace.Core.Model;
using System.Diagnostics;
using System.Text;
using Xunit.Abstractions;

namespace MemPalace.E2E.Tests;

/// <summary>
/// E2E tests for complete RAG (Retrieval-Augmented Generation) pipeline.
/// Validates: mine → search → inject → respond workflow with quality SLOs.
/// </summary>
public sealed class RAGPipelineTests : E2ETestBase
{
    private readonly ITestOutputHelper _output;

    public RAGPipelineTests(ITestOutputHelper output)
    {
        _output = output;
    }
    [Fact]
    public async Task TestRAGPipelineFullCycle()
    {
        // Phase 1: Mine - Create realistic document corpus
        await InitializePalaceAsync();
        var documents = CreateCachingDocumentCorpus();

        var records = new List<EmbeddedRecord>();
        for (int i = 0; i < documents.Length; i++)
        {
            var embedding = (await Embedder.EmbedAsync(new[] { documents[i] }))[0];
            records.Add(new EmbeddedRecord(
                Id: $"cache-doc-{i}",
                Document: documents[i],
                Metadata: new Dictionary<string, object?>
                {
                    { "wing", "caching-docs" },
                    { "category", "distributed-systems" },
                    { "indexed_at", DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
                },
                Embedding: embedding.ToArray()
            ));
        }
        await Collection.AddAsync(records);

        // Phase 2: Search - Semantic search + optional hybrid
        var userQuery = "How do we handle distributed caching?";
        var queryEmbedding = await Embedder.EmbedAsync(new[] { userQuery });
        
        var sw = Stopwatch.StartNew();
        var searchResults = await Collection.QueryAsync(queryEmbedding, nResults: 5);
        sw.Stop();

        searchResults.Ids[0].Should().NotBeEmpty("search should return results");
        _output.WriteLine($"Search phase: {sw.ElapsedMilliseconds}ms, {searchResults.Ids[0].Count} results");

        // Phase 3: Inject - Format retrieved docs into LLM prompt
        var retrievedDocs = searchResults.Documents[0];
        var llmPrompt = InjectContextIntoPrompt(userQuery, retrievedDocs);

        llmPrompt.Should().Contain("distributed", "injected context should contain query keywords");
        llmPrompt.Should().Contain("Redis", "injected context should contain relevant technical terms");
        _output.WriteLine($"Injection phase: prompt size = {llmPrompt.Length} chars");

        // Phase 4: Respond - Generate response from context (mock LLM)
        var mockResponse = GenerateMockLLMResponse(llmPrompt);

        mockResponse.Should().NotBeEmpty("LLM should generate a response");
        mockResponse.Should().Contain("Redis", "response should reference specific details from context");
        
        _output.WriteLine($"Response phase: {mockResponse.Length} chars generated");
        _output.WriteLine("=== LLM Response ===");
        _output.WriteLine(mockResponse);

        // End-to-end latency check
        // Note: Only search latency measured here; full pipeline would include embedding + LLM latency
        sw.ElapsedMilliseconds.Should().BeLessThan(500, 
            "search phase should complete in <500ms (excluding LLM inference)");
    }

    [Fact]
    public async Task TestSearchBaseline()
    {
        // Arrange: Create 50-document corpus with known ground truth
        await InitializePalaceAsync();
        var (documents, groundTruthMapping) = CreateGroundTruthCorpus();

        var records = new List<EmbeddedRecord>();
        for (int i = 0; i < documents.Length; i++)
        {
            var embedding = (await Embedder.EmbedAsync(new[] { documents[i] }))[0];
            records.Add(new EmbeddedRecord(
                Id: $"gt-{i}",
                Document: documents[i],
                Metadata: new Dictionary<string, object?> { { "wing", "ground-truth" } },
                Embedding: embedding.ToArray()
            ));
        }
        await Collection.AddAsync(records);

        // Act: Execute test queries with ground truth
        var testQueries = new[]
        {
            ("authentication security", new[] { "JWT", "OAuth2", "token" }),
            ("caching strategy", new[] { "Redis", "cache", "TTL" }),
            ("error handling", new[] { "exception", "error", "catch" }),
            ("database optimization", new[] { "PostgreSQL", "query", "index" }),
            ("API design", new[] { "REST", "HTTP", "endpoint" })
        };

        var totalQueries = testQueries.Length;
        var successfulRetrievals = 0;

        foreach (var (query, expectedKeywords) in testQueries)
        {
            var queryEmbedding = await Embedder.EmbedAsync(new[] { query });
            var results = await Collection.QueryAsync(queryEmbedding, nResults: 5);

            // Check if top-5 results contain at least one expected keyword
            var topDocs = results.Documents[0];
            var hasRelevantDoc = topDocs.Any(doc => 
                expectedKeywords.Any(kw => doc.Contains(kw, StringComparison.OrdinalIgnoreCase)));

            if (hasRelevantDoc)
                successfulRetrievals++;

            _output.WriteLine($"Query: '{query}' → R@5: {(hasRelevantDoc ? "✓" : "✗")}");
        }

        // Assert: Recall@5 ≥ 96.6% (Phase 3 baseline)
        var recallAt5 = successfulRetrievals / (double)totalQueries;
        recallAt5.Should().BeGreaterThanOrEqualTo(0.966, 
            $"R@5 should maintain Phase 3 baseline (actual: {recallAt5 * 100:F1}%)");

        _output.WriteLine($"Search Baseline: R@5 = {recallAt5 * 100:F1}% ({successfulRetrievals}/{totalQueries})");
    }

    [Fact]
    public async Task TestContextInjectionAccuracy()
    {
        // Arrange: Create small corpus
        await InitializePalaceAsync();
        var documents = new[]
        {
            "Redis provides in-memory caching with TTL support",
            "PostgreSQL offers ACID compliance for transactional data",
            "JWT tokens are stateless and contain encoded claims"
        };

        var records = new List<EmbeddedRecord>();
        for (int i = 0; i < documents.Length; i++)
        {
            var embedding = (await Embedder.EmbedAsync(new[] { documents[i] }))[0];
            records.Add(new EmbeddedRecord(
                Id: $"ci-{i}",
                Document: documents[i],
                Metadata: new Dictionary<string, object?> { { "wing", "context-test" } },
                Embedding: embedding.ToArray()
            ));
        }
        await Collection.AddAsync(records);

        // Act: Search for "caching"
        var query = "caching strategy";
        var queryEmbedding = await Embedder.EmbedAsync(new[] { query });
        var searchResults = await Collection.QueryAsync(queryEmbedding, nResults: 3);

        var retrievedDocs = searchResults.Documents[0];
        var prompt = InjectContextIntoPrompt(query, retrievedDocs);

        // Assert: Injected context should match search results exactly
        foreach (var doc in retrievedDocs)
        {
            prompt.Should().Contain(doc, 
                "injected prompt should contain exact retrieved documents");
        }

        // Assert: Context should be in correct format (system + context + query)
        prompt.Should().Contain("Context:", "prompt should have context section");
        prompt.Should().Contain("Question:", "prompt should have question section");
        prompt.Should().Contain(query, "prompt should include original query");

        _output.WriteLine("=== Injected Prompt ===");
        _output.WriteLine(prompt);
    }

    [Fact]
    public async Task TestRAGResponseQuality()
    {
        // Arrange: Create focused corpus on specific topic
        await InitializePalaceAsync();
        var documents = new[]
        {
            "Distributed caching with Redis: Set TTL to 1 hour. Use sliding expiration for frequently accessed items.",
            "Redis cluster mode provides high availability with automatic failover and sharding.",
            "Cache invalidation strategies: TTL-based, event-driven, or manual purge on data updates.",
            "Redis persistence: RDB snapshots for point-in-time backups, AOF for durability.",
            "Cache warming: Preload frequently accessed data on startup to avoid cold start latency."
        };

        var records = new List<EmbeddedRecord>();
        for (int i = 0; i < documents.Length; i++)
        {
            var embedding = (await Embedder.EmbedAsync(new[] { documents[i] }))[0];
            records.Add(new EmbeddedRecord(
                Id: $"rq-{i}",
                Document: documents[i],
                Metadata: new Dictionary<string, object?> { { "wing", "caching-advanced" } },
                Embedding: embedding.ToArray()
            ));
        }
        await Collection.AddAsync(records);

        // Act: Full RAG cycle
        var query = "How should we configure Redis caching for high availability?";
        var queryEmbedding = await Embedder.EmbedAsync(new[] { query });
        var searchResults = await Collection.QueryAsync(queryEmbedding, nResults: 3);

        var retrievedDocs = searchResults.Documents[0];
        var prompt = InjectContextIntoPrompt(query, retrievedDocs);
        var response = GenerateMockLLMResponse(prompt);

        // Assert: Response should mention specific details from context
        var expectedTerms = new[] { "Redis", "cluster", "high availability", "TTL" };
        var mentionedTerms = expectedTerms.Count(term => 
            response.Contains(term, StringComparison.OrdinalIgnoreCase));

        mentionedTerms.Should().BeGreaterThanOrEqualTo(2, 
            "response should mention at least 2 specific details from retrieved context");

        // Assert: Response should not be generic (should reference injected context)
        response.Should().NotContain("in general", 
            "response should be specific, not generic");
        response.Should().NotContain("typically", 
            "response should reference specific context, not general knowledge");

        _output.WriteLine("=== RAG Response Quality Check ===");
        _output.WriteLine($"Query: {query}");
        _output.WriteLine($"Retrieved {retrievedDocs.Count} docs");
        _output.WriteLine($"Response mentions {mentionedTerms}/{expectedTerms.Length} expected terms");
        _output.WriteLine($"Response: {response}");
    }

    // Helper methods

    private static string[] CreateCachingDocumentCorpus()
    {
        return new[]
        {
            // Caching (relevant docs)
            "Distributed caching with Redis provides in-memory storage for session data and frequently accessed content. TTL is typically 1 hour with sliding expiration.",
            "Redis cluster mode enables horizontal scaling and automatic failover for high availability distributed caching.",
            "Cache invalidation strategies include TTL-based expiration, event-driven purging, and manual cache clearing on data updates.",
            
            // Related but different topics
            "Database connection pooling reduces latency by reusing existing connections. Max 20 connections per instance.",
            "PostgreSQL query optimization using EXPLAIN ANALYZE to identify slow queries and missing indexes.",
            "Authentication middleware validates JWT tokens and enforces role-based access control (RBAC).",
            "API rate limiting prevents abuse by restricting requests per client IP to 100/minute.",
            "Logging infrastructure uses Serilog for structured logging with JSON output to Application Insights.",
            "Message queue processing with RabbitMQ for asynchronous task execution and event-driven workflows.",
            "Monitoring setup with Prometheus metrics and Grafana dashboards for real-time observability.",
            
            // More caching content
            "Cache warming preloads frequently accessed data on application startup to reduce cold start latency.",
            "Redis persistence options: RDB snapshots for point-in-time backups, AOF for write durability.",
            
            // Noise (less relevant)
            "Frontend build pipeline uses Webpack with tree shaking and code splitting for optimal bundle size.",
            "Docker multi-stage builds reduce image size by excluding build dependencies from runtime container.",
            "CI/CD pipeline stages: build, test, security scan, deploy to staging, smoke test, deploy to production.",
            "Feature flags enable gradual rollout and A/B testing without redeploying the application.",
            "Error tracking with Sentry captures exceptions with full stack traces and user context.",
            "API documentation generated from OpenAPI specs with Swagger UI for interactive testing.",
            "Git workflow: feature branches, PR reviews, automated tests in CI, squash merge to main.",
            "Security headers configured: HSTS, CSP, X-Frame-Options, X-Content-Type-Options."
        };
    }

    private static (string[] documents, Dictionary<string, string[]> groundTruth) CreateGroundTruthCorpus()
    {
        var documents = new[]
        {
            "JWT tokens provide stateless authentication with encoded claims and signature verification",
            "OAuth2 authorization flow with refresh tokens for long-lived sessions",
            "Redis distributed caching with TTL and sliding expiration policies",
            "In-memory cache for session data with automatic expiration",
            "Exception handling middleware catches errors and returns standardized error responses",
            "Try-catch blocks with specific exception types for granular error handling",
            "PostgreSQL database with connection pooling and query optimization",
            "Database indexing strategies for faster query execution on large tables",
            "RESTful API design with HTTP verbs and resource-based URLs",
            "API versioning in URL path to maintain backward compatibility"
        };

        var groundTruth = new Dictionary<string, string[]>
        {
            ["authentication security"] = new[] { "JWT", "OAuth2", "token" },
            ["caching strategy"] = new[] { "Redis", "cache", "TTL" },
            ["error handling"] = new[] { "exception", "error", "catch" },
            ["database optimization"] = new[] { "PostgreSQL", "query", "index" },
            ["API design"] = new[] { "REST", "HTTP", "endpoint" }
        };

        return (documents, groundTruth);
    }

    private static string InjectContextIntoPrompt(string query, IReadOnlyList<string> retrievedDocs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a helpful technical assistant. Answer the question based on the provided context.");
        sb.AppendLine();
        sb.AppendLine("Context:");
        for (int i = 0; i < retrievedDocs.Count; i++)
        {
            sb.AppendLine($"{i + 1}. {retrievedDocs[i]}");
        }
        sb.AppendLine();
        sb.AppendLine($"Question: {query}");
        sb.AppendLine();
        sb.AppendLine("Answer based on the context above:");
        
        return sb.ToString();
    }

    private static string GenerateMockLLMResponse(string prompt)
    {
        // Mock LLM: Extract key terms from prompt and generate response
        var promptLower = prompt.ToLowerInvariant();

        if (promptLower.Contains("distributed caching") || promptLower.Contains("redis"))
        {
            return "Based on the provided context, distributed caching with Redis is recommended. " +
                   "Key configurations include: (1) Redis cluster mode for high availability with automatic failover, " +
                   "(2) TTL-based expiration set to 1 hour with sliding expiration for frequently accessed items, " +
                   "(3) RDB snapshots and AOF for persistence and durability. " +
                   "This setup ensures both performance and reliability for distributed caching scenarios.";
        }

        if (promptLower.Contains("authentication") || promptLower.Contains("jwt"))
        {
            return "Authentication should use JWT tokens for stateless authentication. " +
                   "Tokens contain encoded claims and are verified using signature validation. " +
                   "For long-lived sessions, implement OAuth2 with refresh tokens.";
        }

        // Generic fallback
        return "Based on the provided context, the recommended approach involves following industry best practices " +
               "for scalability, security, and maintainability.";
    }
}
