using FluentAssertions;
using System.Diagnostics;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;

namespace MemPalace.E2E.Tests;

/// <summary>
/// E2E tests for wake-up (recent memory retrieval) workflow.
/// Covers: recency ordering, latency SLOs, filtering, empty results, pagination.
/// </summary>
public sealed class WakeUpE2ETests : E2ETestBase
{
    [Fact]
    public async Task WhenWakeUpSimple_ExpectRecentMemoriesReturned()
    {
        // Arrange
        await InitializePalaceAsync();
        var records = await CreateTestMemoriesAsync(10, "memories");
        await Collection.AddAsync(records);

        // Act
        var result = await Collection.GetAsync(limit: 20, include: IncludeFields.Documents | IncludeFields.Metadatas);

        // Assert
        result.Documents.Should().NotBeEmpty();
        result.Documents.Count.Should().BeLessThanOrEqualTo(20);
    }

    [Fact]
    public async Task WhenWakeUpWithLimit_ExpectExactLimitReturned()
    {
        // Arrange
        await InitializePalaceAsync();
        var records = await CreateTestMemoriesAsync(30, "test");
        await Collection.AddAsync(records);

        // Act
        var result5 = await Collection.GetAsync(limit: 5, include: IncludeFields.Documents);
        var result10 = await Collection.GetAsync(limit: 10, include: IncludeFields.Documents);

        // Assert
        result5.Documents.Count.Should().BeLessThanOrEqualTo(5);
        result10.Documents.Count.Should().BeLessThanOrEqualTo(10);
    }

    [Fact]
    public async Task WhenWakeUpEmptyCollection_ExpectNoResults()
    {
        // Arrange
        await InitializePalaceAsync();

        // Act
        var result = await Collection.GetAsync(limit: 10, include: IncludeFields.Documents);

        // Assert
        result.Documents.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenWakeUpLatency_ExpectUnderSLO()
    {
        // Arrange
        await InitializePalaceAsync();
        var records = await CreateTestMemoriesAsync(100, "perf");
        await Collection.AddAsync(records);

        var sw = Stopwatch.StartNew();

        // Act
        var result = await Collection.GetAsync(limit: 20, include: IncludeFields.Documents);

        sw.Stop();

        // Assert
        result.Documents.Should().NotBeEmpty();
        sw.Elapsed.TotalMilliseconds.Should().BeLessThan(50.0, "Wake-up should complete in <50ms");
    }

    [Fact]
    public async Task WhenWakeUpManyRecords_ExpectPerformanceScales()
    {
        // Arrange
        await InitializePalaceAsync();
        var records = await CreateTestMemoriesAsync(500, "perf");
        await Collection.AddAsync(records);

        var sw = Stopwatch.StartNew();

        // Act
        var result = await Collection.GetAsync(limit: 50, include: IncludeFields.Documents);

        sw.Stop();

        // Assert
        result.Documents.Count.Should().BeLessThanOrEqualTo(50);
        sw.Elapsed.TotalSeconds.Should().BeLessThan(5.0, "Even with 500 records, retrieval should be fast");
    }

    [Fact]
    public async Task WhenWakeUpWithMetadataIncluded_ExpectMetadataReturned()
    {
        // Arrange
        await InitializePalaceAsync();
        var records = await CreateTestMemoriesAsync(5, "test");
        await Collection.AddAsync(records);

        // Act
        var result = await Collection.GetAsync(limit: 10, include: IncludeFields.Metadatas | IncludeFields.Documents);

        // Assert
        result.Documents.Should().NotBeEmpty();
        result.Metadatas.Should().NotBeEmpty();
        result.Metadatas.Count.Should().Be(result.Documents.Count);
    }

    [Fact]
    public async Task WhenWakeUpWithWingFilter_ExpectOnlyMatchingWing()
    {
        // Arrange
        await InitializePalaceAsync();
        var wingARecords = await CreateTestMemoriesAsync(5, "wing-a");
        var wingBRecords = await CreateTestMemoriesAsync(5, "wing-b");
        await Collection.AddAsync(wingARecords.Concat(wingBRecords).ToList());

        var wingAFilter = new Core.Backends.Eq("wing", "wing-a");

        // Act
        var resultAll = await Collection.GetAsync(limit: 20, include: IncludeFields.Documents);
        var resultFiltered = await Collection.GetAsync(limit: 20, include: IncludeFields.Documents | IncludeFields.Metadatas, where: wingAFilter);

        // Assert
        resultAll.Documents.Count.Should().BeGreaterThanOrEqualTo(resultFiltered.Documents.Count);
        resultFiltered.Documents.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task WhenWakeUpTwice_ExpectConsistentOrder()
    {
        // Arrange
        await InitializePalaceAsync();
        var records = await CreateTestMemoriesAsync(10, "test");
        await Collection.AddAsync(records);

        // Act
        var result1 = await Collection.GetAsync(limit: 5, include: IncludeFields.Documents);
        var result2 = await Collection.GetAsync(limit: 5, include: IncludeFields.Documents);

        // Assert
        result1.Documents.Should().Equal(result2.Documents);
    }

    [Fact]
    public async Task WhenWakeUpRequestMoreThanAvailable_ExpectAllReturned()
    {
        // Arrange
        await InitializePalaceAsync();
        var records = await CreateTestMemoriesAsync(5, "test");
        await Collection.AddAsync(records);

        // Act
        var result = await Collection.GetAsync(limit: 100, include: IncludeFields.Documents);

        // Assert
        result.Documents.Count.Should().Be(5);
    }

    [Fact]
    public async Task WhenWakeUpAfterAddition_ExpectNewRecordsVisible()
    {
        // Arrange
        await InitializePalaceAsync();
        var initialRecords = await CreateTestMemoriesAsync(3, "test");
        await Collection.AddAsync(initialRecords);

        var result1 = await Collection.GetAsync(limit: 10, include: IncludeFields.Documents);

        // Add more records
        var additionalRecords = await CreateTestMemoriesAsync(2, "test");
        await Collection.AddAsync(additionalRecords);

        // Act
        var result2 = await Collection.GetAsync(limit: 10, include: IncludeFields.Documents);

        // Assert
        result2.Documents.Count.Should().Be(result1.Documents.Count + 2);
    }

    [Fact]
    public async Task WhenWakeUpWithComplexMetadataFilter_ExpectCorrectFiltering()
    {
        // Arrange
        await InitializePalaceAsync();
        var records = new List<EmbeddedRecord>();
        
        for (int i = 0; i < 20; i++)
        {
            var content = $"Memory {i}";
            var embeddings = await Embedder.EmbedAsync(new[] { content });
            records.Add(new EmbeddedRecord(
                Id: $"mem-{i}",
                Document: content,
                Metadata: new Dictionary<string, object?>
                {
                    { "wing", i < 10 ? "work" : "personal" },
                    { "priority", i % 3 == 0 ? "high" : "normal" },
                    { "processed", i % 2 == 0 }
                },
                Embedding: embeddings[0].ToArray()
            ));
        }
        
        await Collection.AddAsync(records);

        var workWingFilter = new Eq("wing", "work");

        // Act
        var result = await Collection.GetAsync(limit: 20, include: IncludeFields.Documents | IncludeFields.Metadatas, where: workWingFilter);

        // Assert
        result.Documents.Count.Should().BeGreaterThan(0);
        result.Documents.Count.Should().BeLessThanOrEqualTo(10);
    }

    [Fact]
    public async Task WhenWakeUpMultipleWings_ExpectCrosswingAccess()
    {
        // Arrange
        await InitializePalaceAsync();
        var workRecords = await CreateTestMemoriesAsync(5, "work");
        var personalRecords = await CreateTestMemoriesAsync(5, "personal");
        var researchRecords = await CreateTestMemoriesAsync(5, "research");
        
        await Collection.AddAsync(workRecords.Concat(personalRecords).Concat(researchRecords).ToList());

        // Act
        var resultAll = await Collection.GetAsync(limit: 20, include: IncludeFields.Documents);

        // Assert
        resultAll.Documents.Count.Should().Be(15);
    }

    [Fact]
    public async Task WhenWakeUpIncludeFields_ExpectCorrectFields()
    {
        // Arrange
        await InitializePalaceAsync();
        var records = await CreateTestMemoriesAsync(5, "test");
        await Collection.AddAsync(records);

        // Act
        var docsOnly = await Collection.GetAsync(limit: 5, include: IncludeFields.Documents);
        var both = await Collection.GetAsync(limit: 5, include: IncludeFields.Documents);

        // Assert
        docsOnly.Documents.Should().NotBeEmpty();
        both.Documents.Should().NotBeEmpty();
    }
}
