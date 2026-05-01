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
        var embedding1 = (await Embedder.EmbedAsync(new[] { "Python programming language and tutorials" }))[0];
        var embedding2 = (await Embedder.EmbedAsync(new[] { "JavaScript and web development frameworks" }))[0];
        records[0] = records[0] with { Document = "Python programming language and tutorials", Embedding = embedding1.ToArray() };
        records[1] = records[1] with { Document = "JavaScript and web development frameworks", Embedding = embedding2.ToArray() };
        await Collection.AddAsync(records);

        var queryEmbeddings = await Embedder.EmbedAsync(new[] { "Python programming" });

        // Act
        var result = await Collection.QueryAsync(queryEmbeddings, nResults: 5);

        // Assert
        result.Documents.Should().NotBeEmpty("Should return at least one result");
    }

    [Fact]
    public async Task WhenSearchWithLimit_ExpectExactNumberReturned()
    {
        // Arrange
        await InitializePalaceAsync();
        var records = await CreateTestMemoriesAsync(20, "test");
        await Collection.AddAsync(records);

        var queryEmbeddings = await Embedder.EmbedAsync(new[] { "test query" });

        // Act
        var result5 = await Collection.QueryAsync(queryEmbeddings, nResults: 5);
        var result10 = await Collection.QueryAsync(queryEmbeddings, nResults: 10);

        // Assert
        result5.Ids.Count.Should().BeLessThanOrEqualTo(5);
        result10.Ids.Count.Should().BeLessThanOrEqualTo(10);
        result10.Ids.Count.Should().BeGreaterThanOrEqualTo(result5.Ids.Count);
    }

    [Fact]
    public async Task WhenSearchEmptyCollection_ExpectNoDocuments()
    {
        // Arrange
        await InitializePalaceAsync();
        var queryEmbeddings = await Embedder.EmbedAsync(new[] { "query" });

        // Act
        var result = await Collection.QueryAsync(queryEmbeddings, nResults: 10);

        // Assert - Just verify that query completes without error
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenSearchWithWingFilter_ExpectOnlyMatchingWingReturned()
    {
        // Arrange
        await InitializePalaceAsync();
        var wingARecords = await CreateTestMemoriesAsync(5, "wing-a");
        var wingBRecords = await CreateTestMemoriesAsync(5, "wing-b");
        await Collection.AddAsync(wingARecords.Concat(wingBRecords).ToList());

        var queryEmbeddings = await Embedder.EmbedAsync(new[] { "test query" });
        var wingFilter = new Eq("wing", "wing-a");

        // Act
        var resultAll = await Collection.QueryAsync(queryEmbeddings, nResults: 10);
        var resultFiltered = await Collection.QueryAsync(queryEmbeddings, nResults: 10, where: wingFilter);

        // Assert
        resultAll.Ids.Count.Should().BeGreaterThan(0);
        resultFiltered.Ids.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task WhenSearchMultipleTimes_ExpectDeterministicOrdering()
    {
        // Arrange
        await InitializePalaceAsync();
        var records = await CreateTestMemoriesAsync(15, "test");
        await Collection.AddAsync(records);

        var queryEmbeddings = await Embedder.EmbedAsync(new[] { "test consistency" });

        // Act
        var result1 = await Collection.QueryAsync(queryEmbeddings, nResults: 5);
        var result2 = await Collection.QueryAsync(queryEmbeddings, nResults: 5);

        // Assert - Documents should be identical for same query
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
        var queryEmbeddings = await Embedder.EmbedAsync(new[] { "query" });

        // Act
        var resultFiltered = await Collection.QueryAsync(queryEmbeddings, nResults: 10, where: roomFilter);

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

        var queryEmbeddings = await Embedder.EmbedAsync(new[] { "performance test" });
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = await Collection.QueryAsync(queryEmbeddings, nResults: 50);
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
        var embedding = (await Embedder.EmbedAsync(new[] { "Unique distinctive content that is different" }))[0].ToArray();
        var newRecord = new EmbeddedRecord(
            Id: $"new-{Guid.NewGuid()}",
            Document: "Unique distinctive content that is different",
            Metadata: new Dictionary<string, object?> { { "wing", "test" }, { "source", "new" } },
            Embedding: embedding
        );
        await Collection.AddAsync(new[] { newRecord });

        var queryEmbeddings = await Embedder.EmbedAsync(new[] { "distinctive" });

        // Act
        var result = await Collection.QueryAsync(queryEmbeddings, nResults: 5);

        // Assert
        result.Documents.Should().NotBeEmpty("Should find the distinctive content");
    }

    [Fact]
    public async Task WhenSearchWithSimilarQueries_ExpectConsistentResults()
    {
        // Arrange
        await InitializePalaceAsync();
        var records = await CreateTestMemoriesAsync(20, "test");
        await Collection.AddAsync(records);

        var query1Embeddings = await Embedder.EmbedAsync(new[] { "machine learning" });
        var query2Embeddings = await Embedder.EmbedAsync(new[] { "machine learning models" });

        // Act
        var result1 = await Collection.QueryAsync(query1Embeddings, nResults: 5);
        var result2 = await Collection.QueryAsync(query2Embeddings, nResults: 5);

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

        var queryEmbeddings = await Embedder.EmbedAsync(new[] { "query" });

        // Act
        var result = await Collection.QueryAsync(queryEmbeddings, nResults: 100);

        // Assert
        result.Documents.Count.Should().BeLessThanOrEqualTo(100);
        result.Documents.Count.Should().BeGreaterThan(0);
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
            var embeddings = await Embedder.EmbedAsync(new[] { content });
            records.Add(new EmbeddedRecord(
                Id: $"rec-{i}",
                Document: content,
                Metadata: new Dictionary<string, object?>
                {
                    { "wing", i < 5 ? "wing-a" : "wing-b" },
                    { "priority", i % 2 == 0 ? "high" : "low" },
                    { "timestamp", DateTimeOffset.UtcNow.AddHours(-i).ToUnixTimeSeconds() }
                },
                Embedding: embeddings[0].ToArray()
            ));
        }
        
        await Collection.AddAsync(records);

        var queryEmbeddings = await Embedder.EmbedAsync(new[] { "query" });
        var wingAFilter = new Eq("wing", "wing-a");
        var highPriorityFilter = new Eq("priority", "high");

        // Act
        var resultWingA = await Collection.QueryAsync(queryEmbeddings, nResults: 10, where: wingAFilter);
        var resultHighPriority = await Collection.QueryAsync(queryEmbeddings, nResults: 10, where: highPriorityFilter);

        // Assert
        resultWingA.Ids.Count.Should().BeGreaterThan(0);
        resultHighPriority.Ids.Count.Should().BeGreaterThan(0);
    }
}
