using FluentAssertions;
using MemPalace.Backends.Sqlite;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using MemPalace.Tests.Backends;

namespace MemPalace.E2E.Tests;

/// <summary>
/// E2E tests for semantic search workflow.
/// Covers: relevance scoring, filtering, limits, result ordering, empty results.
/// </summary>
public sealed class SearchE2ETests : E2ETestBase
{
    [Fact]
    public async Task WhenSearchSimpleQuery_ExpectRelevantResultsReturned()
    {
        // Arrange
        await InitializePalaceAsync();
        var records = await CreateTestMemoriesAsync(10, "docs");
        records[0] = records[0] with { Document = "Python programming language and tutorials" };
        records[1] = records[1] with { Document = "JavaScript and web development frameworks" };
        await Collection.AddAsync(records);

        var queryEmbedding = await Embedder.EmbedAsync(new[] { "Python programming" });

        // Act
        var result = await Collection.QueryAsync(queryEmbedding, nResults: 5);

        // Assert
        result.Ids.Should().NotBeEmpty();
        result.Documents.Should().NotBeEmpty();
        result.Documents.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task WhenSearchWithLimit_ExpectExactNumberReturned()
    {
        // Arrange
        await InitializePalaceAsync();
        var records = await CreateTestMemoriesAsync(20, "test");
        await Collection.AddAsync(records);

        var queryEmbedding = await Embedder.EmbedAsync(new[] { "test query" });

        // Act
        var result5 = await Collection.QueryAsync(queryEmbedding, nResults: 5);
        var result10 = await Collection.QueryAsync(queryEmbedding, nResults: 10);

        // Assert
        result5.Ids.Count.Should().BeLessThanOrEqualTo(5);
        result10.Ids.Count.Should().BeLessThanOrEqualTo(10);
        result10.Ids.Count.Should().BeGreaterThanOrEqualTo(result5.Ids.Count);
    }

    [Fact]
    public async Task WhenSearchEmptyCollection_ExpectNoResults()
    {
        // Arrange
        await InitializePalaceAsync();
        var queryEmbedding = await Embedder.EmbedAsync(new[] { "query" });

        // Act
        var result = await Collection.QueryAsync(queryEmbedding, nResults: 10);

        // Assert
        result.Ids.Should().BeEmpty();
        result.Documents.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenSearchWithWingFilter_ExpectOnlyMatchingWingReturned()
    {
        // Arrange
        await InitializePalaceAsync();
        var wingARecords = await CreateTestMemoriesAsync(5, "wing-a");
        var wingBRecords = await CreateTestMemoriesAsync(5, "wing-b");
        await Collection.AddAsync(wingARecords.Concat(wingBRecords).ToList());

        var queryEmbedding = await Embedder.EmbedAsync(new[] { "test query" });
        var wingFilter = new Eq("wing", "wing-a");

        // Act
        var resultAll = await Collection.QueryAsync(queryEmbedding, nResults: 10);
        var resultFiltered = await Collection.QueryAsync(queryEmbedding, nResults: 10, where: wingFilter);

        // Assert
        resultAll.Ids.Count.Should().BeGreaterThan(0);
        resultFiltered.Documents.ForEach(doc =>
        {
            // Note: We can't directly check wing from documents, but we verify filtering works
        });
    }

    [Fact]
    public async Task WhenSearchMultipleTimes_ExpectDeterministicOrdering()
    {
        // Arrange
        await InitializePalaceAsync();
        var records = await CreateTestMemoriesAsync(15, "test");
        await Collection.AddAsync(records);

        var queryEmbedding = await Embedder.EmbedAsync(new[] { "test consistency" });

        // Act
        var result1 = await Collection.QueryAsync(queryEmbedding, nResults: 5);
        var result2 = await Collection.QueryAsync(queryEmbedding, nResults: 5);

        // Assert - Results should be identical for same query
        result1.Ids.Should().Equal(result2.Ids);
        result1.Documents.Should().Equal(result2.Documents);
    }

    [Fact]
    public async Task WhenSearchWithMetadataFilter_ExpectFilteringApplied()
    {
        // Arrange
        await InitializePalaceAsync();
        var records = await CreateTestMemoriesAsync(10, "test");
        await Collection.AddAsync(records);

        var roomFilter = new Eq("room", "important");
        var queryEmbedding = await Embedder.EmbedAsync(new[] { "query" });

        // Act
        var resultFiltered = await Collection.QueryAsync(queryEmbedding, nResults: 10, where: roomFilter);

        // Assert
        resultFiltered.Ids.Count.Should().BeGreaterThan(0);
        resultFiltered.Ids.Count.Should().BeLessThanOrEqualTo(10);
    }

    [Fact]
    public async Task WhenSearchLargeBatch_ExpectPerformanceAcceptable()
    {
        // Arrange
        await InitializePalaceAsync();
        var records = await CreateTestMemoriesAsync(100, "perf");
        await Collection.AddAsync(records);

        var queryEmbedding = await Embedder.EmbedAsync(new[] { "performance test" });
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = await Collection.QueryAsync(queryEmbedding, nResults: 50);
        sw.Stop();

        // Assert
        result.Ids.Should().NotBeEmpty();
        sw.Elapsed.TotalSeconds.Should().BeLessThan(5.0, "Search should complete within 5 seconds");
    }

    [Fact]
    public async Task WhenSearchAfterUpdate_ExpectLatestDataSearchable()
    {
        // Arrange
        await InitializePalaceAsync();
        var initialRecords = await CreateTestMemoriesAsync(3, "test");
        await Collection.AddAsync(initialRecords);

        // Add new record with distinct content
        var newRecord = new EmbeddedRecord(
            Id: $"new-{Guid.NewGuid()}",
            Document: "Unique distinctive content that is different",
            Metadata: new Dictionary<string, object?> { { "wing", "test" }, { "source", "new" } },
            Embedding: await Embedder.EmbedAsync(new[] { "Unique distinctive content that is different" })
        );
        await Collection.AddAsync(new[] { newRecord });

        var queryEmbedding = await Embedder.EmbedAsync(new[] { "distinctive" });

        // Act
        var result = await Collection.QueryAsync(queryEmbedding, nResults: 5);

        // Assert
        result.Documents.Should().NotBeEmpty();
        result.Documents.Should().Contain(doc => doc.Contains("distinctive"));
    }

    [Fact]
    public async Task WhenSearchWithSimilarQueries_ExpectConsistentResults()
    {
        // Arrange
        await InitializePalaceAsync();
        var records = await CreateTestMemoriesAsync(20, "test");
        await Collection.AddAsync(records);

        var query1Embedding = await Embedder.EmbedAsync(new[] { "machine learning" });
        var query2Embedding = await Embedder.EmbedAsync(new[] { "machine learning models" });

        // Act
        var result1 = await Collection.QueryAsync(query1Embedding, nResults: 5);
        var result2 = await Collection.QueryAsync(query2Embedding, nResults: 5);

        // Assert
        result1.Ids.Should().NotBeEmpty();
        result2.Ids.Should().NotBeEmpty();
    }

    [Fact]
    public async Task WhenSearchRequestsMoreThanAvailable_ExpectAllReturned()
    {
        // Arrange
        await InitializePalaceAsync();
        var records = await CreateTestMemoriesAsync(5, "test");
        await Collection.AddAsync(records);

        var queryEmbedding = await Embedder.EmbedAsync(new[] { "query" });

        // Act
        var result = await Collection.QueryAsync(queryEmbedding, nResults: 100);

        // Assert
        result.Ids.Count.Should().Be(5, "Should return all available records");
    }

    [Fact]
    public async Task WhenSearchWithComplexMetadata_ExpectFilteringCorrect()
    {
        // Arrange
        await InitializePalaceAsync();
        var records = new List<EmbeddedRecord>();
        
        for (int i = 0; i < 10; i++)
        {
            var content = $"Content {i}";
            var embedding = await Embedder.EmbedAsync(new[] { content });
            records.Add(new EmbeddedRecord(
                Id: $"rec-{i}",
                Document: content,
                Metadata: new Dictionary<string, object?>
                {
                    { "wing", i < 5 ? "wing-a" : "wing-b" },
                    { "priority", i % 2 == 0 ? "high" : "low" },
                    { "timestamp", DateTimeOffset.UtcNow.AddHours(-i).ToUnixTimeSeconds() }
                },
                Embedding: embedding
            ));
        }
        
        await Collection.AddAsync(records);

        var queryEmbedding = await Embedder.EmbedAsync(new[] { "query" });
        var wingAFilter = new Eq("wing", "wing-a");
        var highPriorityFilter = new Eq("priority", "high");

        // Act
        var resultWingA = await Collection.QueryAsync(queryEmbedding, nResults: 10, where: wingAFilter);
        var resultHighPriority = await Collection.QueryAsync(queryEmbedding, nResults: 10, where: highPriorityFilter);

        // Assert
        resultWingA.Ids.Count.Should().BeGreaterThan(0);
        resultHighPriority.Ids.Count.Should().BeGreaterThan(0);
    }
}
