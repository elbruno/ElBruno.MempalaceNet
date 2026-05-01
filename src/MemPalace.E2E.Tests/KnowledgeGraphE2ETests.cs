using FluentAssertions;
using MemPalace.KnowledgeGraph;

namespace MemPalace.E2E.Tests;

/// <summary>
/// E2E tests for knowledge graph operations.
/// Covers: entity/relationship creation, querying, temporal validity, wildcards.
/// </summary>
public sealed class KnowledgeGraphE2ETests : IAsyncDisposable
{
    private readonly string _tempDbPath;
    private readonly SqliteKnowledgeGraph _kg;

    public KnowledgeGraphE2ETests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"kg-e2e-{Guid.NewGuid()}.db");
        _kg = new SqliteKnowledgeGraph(_tempDbPath);
    }

    public async ValueTask DisposeAsync()
    {
        await _kg.DisposeAsync();
        if (File.Exists(_tempDbPath))
            File.Delete(_tempDbPath);
    }

    [Fact]
    public async Task WhenAddSimpleTriple_ExpectStored()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var triple = new Triple(
            new EntityRef("agent", "alice"),
            "developed",
            new EntityRef("project", "mempalacenet"),
            null
        );
        var temporal = new TemporalTriple(triple, now, null, now);

        // Act
        var id = await _kg.AddAsync(temporal);

        // Assert
        id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task WhenAddAndQueryTriple_ExpectRetrieved()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var triple = new Triple(
            new EntityRef("person", "bob"),
            "works-on",
            new EntityRef("team", "backend"),
            null
        );
        var temporal = new TemporalTriple(triple, now, null, now);
        await _kg.AddAsync(temporal);

        // Act
        var pattern = new TriplePattern(
            new EntityRef("person", "bob"),
            "works-on",
            new EntityRef("team", "backend")
        );
        var results = await _kg.QueryAsync(pattern);

        // Assert
        results.Should().HaveCount(1);
        results[0].Triple.Subject.Should().Be(new EntityRef("person", "bob"));
        results[0].Triple.Predicate.Should().Be("works-on");
        results[0].Triple.Object.Should().Be(new EntityRef("team", "backend"));
    }

    [Fact]
    public async Task WhenAddMultipleTriples_ExpectAllRetrievable()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var triples = new[]
        {
            new TemporalTriple(
                new Triple(new EntityRef("agent", "alice"), "knows", new EntityRef("agent", "bob"), null),
                now, null, now),
            new TemporalTriple(
                new Triple(new EntityRef("agent", "bob"), "knows", new EntityRef("agent", "charlie"), null),
                now, null, now),
            new TemporalTriple(
                new Triple(new EntityRef("agent", "charlie"), "knows", new EntityRef("agent", "alice"), null),
                now, null, now)
        };

        // Act
        foreach (var t in triples)
            await _kg.AddAsync(t);

        var allResults = await _kg.QueryAsync(new TriplePattern(null, "knows", null));

        // Assert
        allResults.Should().HaveCount(3);
    }

    [Fact]
    public async Task WhenQueryWithSubjectWildcard_ExpectAllMatching()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("project", "core"), "uses", new EntityRef("lib", "ai"), null),
            now, null, now));
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("project", "cli"), "uses", new EntityRef("lib", "ai"), null),
            now, null, now));

        // Act
        var results = await _kg.QueryAsync(new TriplePattern(null, "uses", new EntityRef("lib", "ai")));

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task WhenQueryWithPredicateWildcard_ExpectAllMatching()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var subject = new EntityRef("agent", "dev");
        await _kg.AddAsync(new TemporalTriple(
            new Triple(subject, "coded", new EntityRef("file", "main.cs"), null),
            now, null, now));
        await _kg.AddAsync(new TemporalTriple(
            new Triple(subject, "reviewed", new EntityRef("file", "main.cs"), null),
            now, null, now));

        // Act
        var results = await _kg.QueryAsync(new TriplePattern(subject, null, new EntityRef("file", "main.cs")));

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task WhenQueryWithObjectWildcard_ExpectAllMatching()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var subject = new EntityRef("person", "manager");
        await _kg.AddAsync(new TemporalTriple(
            new Triple(subject, "leads", new EntityRef("team", "backend"), null),
            now, null, now));
        await _kg.AddAsync(new TemporalTriple(
            new Triple(subject, "leads", new EntityRef("team", "frontend"), null),
            now, null, now));

        // Act
        var results = await _kg.QueryAsync(new TriplePattern(subject, "leads", null));

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task WhenAddWithTemporalValidity_ExpectTemporalDataPreserved()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var validFrom = now.AddDays(-10);
        var validTo = now.AddDays(10);
        
        var triple = new Triple(
            new EntityRef("role", "senior-eng"),
            "assigned-to",
            new EntityRef("person", "alice"),
            null
        );
        var temporal = new TemporalTriple(triple, validFrom, validTo, now);

        // Act
        await _kg.AddAsync(temporal);
        var results = await _kg.QueryAsync(new TriplePattern(
            new EntityRef("role", "senior-eng"),
            "assigned-to",
            new EntityRef("person", "alice")
        ));

        // Assert
        results.Should().HaveCount(1);
        results[0].ValidFrom.Should().Be(validFrom);
        results[0].ValidTo.Should().Be(validTo);
    }

    [Fact]
    public async Task WhenQueryAsOfTime_ExpectTemporalFiltering()
    {
        // Arrange
        var baseTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        
        // Add triple valid from Jan 1
        var future = new TemporalTriple(
            new Triple(new EntityRef("event", "conference"), "planned-for", new EntityRef("month", "july"), null),
            baseTime.AddMonths(1),  // Valid from Feb 1
            baseTime.AddMonths(6),  // Valid until July 1
            baseTime
        );
        await _kg.AddAsync(future);

        // Query at different times
        var pattern = new TriplePattern(null, "planned-for", null);
        
        var beforeValid = await _kg.QueryAsync(pattern, at: baseTime);
        var duringValid = await _kg.QueryAsync(pattern, at: baseTime.AddMonths(2));
        var afterValid = await _kg.QueryAsync(pattern, at: baseTime.AddMonths(7));

        // Assert
        beforeValid.Should().BeEmpty("Event should not exist before validity");
        duringValid.Should().HaveCount(1, "Event should exist during validity");
        afterValid.Should().BeEmpty("Event should not exist after validity");
    }

    [Fact]
    public async Task WhenAddMultipleRelationshipsForEntity_ExpectAllRetrievable()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var alice = new EntityRef("person", "alice");
        
        var triples = new[]
        {
            new TemporalTriple(
                new Triple(alice, "manages", new EntityRef("team", "core"), null),
                now, null, now),
            new TemporalTriple(
                new Triple(alice, "mentors", new EntityRef("person", "bob"), null),
                now, null, now),
            new TemporalTriple(
                new Triple(alice, "authored", new EntityRef("doc", "design"), null),
                now, null, now)
        };

        // Act
        foreach (var t in triples)
            await _kg.AddAsync(t);

        var results = await _kg.QueryAsync(new TriplePattern(alice, null, null));

        // Assert
        results.Should().HaveCount(3);
        results.Select(r => r.Triple.Predicate).Should().Contain("manages").And.Contain("mentors").And.Contain("authored");
    }

    [Fact]
    public async Task WhenQueryComplexPattern_ExpectMultipleResults()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var framework = new EntityRef("lib", "extensions-ai");
        
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("project", "mempalace"), "uses", framework, null),
            now, null, now));
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("project", "other"), "uses", framework, null),
            now, null, now));
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("project", "third"), "uses", framework, null),
            now, null, now));

        // Act
        var results = await _kg.QueryAsync(new TriplePattern(null, "uses", framework));

        // Assert
        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task WhenAddDuplicateTriple_ExpectMultipleIds()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var triple = new Triple(
            new EntityRef("agent", "test"),
            "duplicate",
            new EntityRef("target", "same"),
            null
        );

        // Act
        var id1 = await _kg.AddAsync(new TemporalTriple(triple, now, null, now));
        var id2 = await _kg.AddAsync(new TemporalTriple(triple, now.AddSeconds(1), null, now.AddSeconds(1)));

        // Assert
        id1.Should().NotBe(id2, "Duplicate triples should have different IDs");
    }

    [Fact]
    public async Task WhenQueryWithAllWildcards_ExpectAllTriples()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("a", "1"), "rel1", new EntityRef("b", "1"), null),
            now, null, now));
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("a", "2"), "rel2", new EntityRef("b", "2"), null),
            now, null, now));

        // Act
        var results = await _kg.QueryAsync(new TriplePattern(null, null, null));

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task WhenEntityRefsWithSpecialChars_ExpectPreserved()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var complexRef = new EntityRef("entity-type:v2", "id-with-dashes_and_underscores");
        
        var triple = new Triple(
            complexRef,
            "related-to",
            new EntityRef("other:type", "another-id"),
            null
        );
        var temporal = new TemporalTriple(triple, now, null, now);

        // Act
        await _kg.AddAsync(temporal);
        var results = await _kg.QueryAsync(new TriplePattern(complexRef, null, null));

        // Assert
        results.Should().HaveCount(1);
        results[0].Triple.Subject.Should().Be(complexRef);
    }
}
