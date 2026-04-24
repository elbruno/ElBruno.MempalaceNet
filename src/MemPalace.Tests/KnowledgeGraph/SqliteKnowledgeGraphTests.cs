using FluentAssertions;
using MemPalace.KnowledgeGraph;

namespace MemPalace.Tests.KnowledgeGraph;

public sealed class SqliteKnowledgeGraphTests : IAsyncDisposable
{
    private readonly string _tempDbPath;
    private readonly SqliteKnowledgeGraph _kg;

    public SqliteKnowledgeGraphTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"kg-test-{Guid.NewGuid()}.db");
        _kg = new SqliteKnowledgeGraph(_tempDbPath);
    }

    public async ValueTask DisposeAsync()
    {
        await _kg.DisposeAsync();
        if (File.Exists(_tempDbPath))
        {
            File.Delete(_tempDbPath);
        }
    }

    [Fact]
    public async Task AddAsync_StoresTripleAndReturnsId()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var triple = new Triple(
            new EntityRef("agent", "tyrell"),
            "worked-on",
            new EntityRef("project", "MemPalace.Core"),
            null
        );
        var temporal = new TemporalTriple(triple, now, null, now);

        // Act
        var id = await _kg.AddAsync(temporal);

        // Assert
        id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task QueryAsync_ReturnsAddedTriple()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var triple = new Triple(
            new EntityRef("agent", "roy"),
            "integrated",
            new EntityRef("library", "Microsoft.Extensions.AI"),
            null
        );
        var temporal = new TemporalTriple(triple, now, null, now);
        await _kg.AddAsync(temporal);

        // Act - query with full pattern
        var results = await _kg.QueryAsync(new TriplePattern(
            new EntityRef("agent", "roy"),
            "integrated",
            new EntityRef("library", "Microsoft.Extensions.AI")
        ));

        // Assert
        results.Should().HaveCount(1);
        results[0].Triple.Subject.Should().Be(new EntityRef("agent", "roy"));
        results[0].Triple.Predicate.Should().Be("integrated");
        results[0].Triple.Object.Should().Be(new EntityRef("library", "Microsoft.Extensions.AI"));
    }

    [Fact]
    public async Task QueryAsync_SubjectWildcard_ReturnsAllMatchingTriples()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("agent", "tyrell"), "worked-on", new EntityRef("project", "Core"), null),
            now, null, now));
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("agent", "roy"), "worked-on", new EntityRef("project", "Core"), null),
            now, null, now));

        // Act - wildcard subject
        var results = await _kg.QueryAsync(new TriplePattern(null, "worked-on", new EntityRef("project", "Core")));

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task QueryAsync_PredicateWildcard_ReturnsAllMatchingTriples()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("agent", "rachael"), "designed", new EntityRef("project", "CLI"), null),
            now, null, now));
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("agent", "rachael"), "implemented", new EntityRef("project", "CLI"), null),
            now, null, now));

        // Act - wildcard predicate
        var results = await _kg.QueryAsync(new TriplePattern(new EntityRef("agent", "rachael"), null, new EntityRef("project", "CLI")));

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task QueryAsync_ObjectWildcard_ReturnsAllMatchingTriples()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("agent", "deckard"), "created", new EntityRef("doc", "architecture.md"), null),
            now, null, now));
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("agent", "deckard"), "created", new EntityRef("doc", "design.md"), null),
            now, null, now));

        // Act - wildcard object
        var results = await _kg.QueryAsync(new TriplePattern(new EntityRef("agent", "deckard"), "created", null));

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task QueryAsync_AllWildcards_ReturnsAllTriples()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("agent", "tyrell"), "worked-on", new EntityRef("project", "Core"), null),
            now, null, now));
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("agent", "roy"), "worked-on", new EntityRef("project", "Ai"), null),
            now, null, now));
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("agent", "rachael"), "designed", new EntityRef("project", "CLI"), null),
            now, null, now));

        // Act - all wildcards
        var results = await _kg.QueryAsync(new TriplePattern(null, null, null));

        // Assert
        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task QueryAsync_PredicateOnlyPattern_ReturnsMatching()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("agent", "tyrell"), "decided", new EntityRef("decision", "tech-stack"), null),
            now, null, now));
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("agent", "bruno"), "decided", new EntityRef("decision", "local-embeddings"), null),
            now, null, now));
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("agent", "roy"), "implemented", new EntityRef("project", "Ai"), null),
            now, null, now));

        // Act - predicate-only pattern
        var results = await _kg.QueryAsync(new TriplePattern(null, "decided", null));

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.Triple.Predicate.Should().Be("decided"));
    }

    [Fact]
    public async Task QueryAsync_ObjectOnlyPattern_ReturnsMatching()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("agent", "tyrell"), "worked-on", new EntityRef("project", "Backend"), null),
            now, null, now));
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("agent", "roy"), "reviewed", new EntityRef("project", "Backend"), null),
            now, null, now));
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("agent", "rachael"), "tested", new EntityRef("project", "CLI"), null),
            now, null, now));

        // Act - object-only pattern
        var results = await _kg.QueryAsync(new TriplePattern(null, null, new EntityRef("project", "Backend")));

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.Triple.Object.Should().Be(new EntityRef("project", "Backend")));
    }

    [Fact]
    public async Task QueryAsync_WithAtTime_ReturnsOnlyValidTriples()
    {
        // Arrange
        var jan1 = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var mar1 = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var jun1 = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var dec1 = new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero);

        // Triple valid from Jan 1 to Jun 1
        var triple = new Triple(
            new EntityRef("agent", "tyrell"),
            "worked-on",
            new EntityRef("phase", "Phase1"),
            null
        );
        await _kg.AddAsync(new TemporalTriple(triple, jan1, jun1, jan1));

        // Act & Assert - query at Mar 1 (should match)
        var resultsInRange = await _kg.QueryAsync(new TriplePattern(null, null, null), at: mar1);
        resultsInRange.Should().HaveCount(1);

        // Act & Assert - query at Dec 1 (should not match)
        var resultsOutOfRange = await _kg.QueryAsync(new TriplePattern(null, null, null), at: dec1);
        resultsOutOfRange.Should().BeEmpty();
    }

    [Fact]
    public async Task EndValidityAsync_SetsEndTime()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var triple = new Triple(
            new EntityRef("agent", "roy"),
            "working-on",
            new EntityRef("phase", "Phase3"),
            null
        );
        var id = await _kg.AddAsync(new TemporalTriple(triple, now, null, now));

        // Act
        var endTime = now.AddHours(2);
        var updated = await _kg.EndValidityAsync(id, endTime);

        // Assert
        updated.Should().BeTrue();

        // Verify triple is no longer valid after end time
        var results = await _kg.QueryAsync(new TriplePattern(null, null, null), at: endTime.AddMinutes(1));
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task EndValidityAsync_WhenAlreadyEnded_ReturnsFalse()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var triple = new Triple(
            new EntityRef("agent", "rachael"),
            "completed",
            new EntityRef("phase", "Phase5"),
            null
        );
        var id = await _kg.AddAsync(new TemporalTriple(triple, now, now.AddHours(1), now));

        // Act - try to end again
        var updated = await _kg.EndValidityAsync(id, now.AddHours(2));

        // Assert
        updated.Should().BeFalse();
    }

    [Fact]
    public async Task TimelineAsync_ReturnsOutgoingAndIncomingEvents()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var entity = new EntityRef("agent", "tyrell");

        // Outgoing: tyrell -> worked-on -> Core
        await _kg.AddAsync(new TemporalTriple(
            new Triple(entity, "worked-on", new EntityRef("project", "Core"), null),
            now, null, now));

        // Incoming: bruno -> assigned -> tyrell
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("lead", "bruno"), "assigned", entity, null),
            now.AddHours(1), null, now.AddHours(1)));

        // Act
        var timeline = await _kg.TimelineAsync(entity);

        // Assert
        timeline.Should().HaveCount(2);
        timeline[0].Direction.Should().Be("outgoing");
        timeline[0].Other.Should().Be(new EntityRef("project", "Core"));
        timeline[1].Direction.Should().Be("incoming");
        timeline[1].Other.Should().Be(new EntityRef("lead", "bruno"));
    }

    [Fact]
    public async Task TimelineAsync_OrdersByValidFromAsc()
    {
        // Arrange
        var t1 = new DateTimeOffset(2026, 4, 24, 10, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2026, 4, 24, 14, 0, 0, TimeSpan.Zero);
        var t3 = new DateTimeOffset(2026, 4, 24, 16, 0, 0, TimeSpan.Zero);
        var entity = new EntityRef("agent", "roy");

        await _kg.AddAsync(new TemporalTriple(
            new Triple(entity, "started", new EntityRef("phase", "Phase3"), null),
            t2, null, t2));
        await _kg.AddAsync(new TemporalTriple(
            new Triple(entity, "planned", new EntityRef("phase", "Phase3"), null),
            t1, null, t1));
        await _kg.AddAsync(new TemporalTriple(
            new Triple(entity, "completed", new EntityRef("phase", "Phase3"), null),
            t3, null, t3));

        // Act
        var timeline = await _kg.TimelineAsync(entity);

        // Assert
        timeline.Should().HaveCount(3);
        timeline[0].At.Should().Be(t1);
        timeline[1].At.Should().Be(t2);
        timeline[2].At.Should().Be(t3);
    }

    [Fact]
    public async Task TimelineAsync_WithFromFilter_ReturnsEventsAfterFrom()
    {
        // Arrange
        var t1 = new DateTimeOffset(2026, 4, 24, 10, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2026, 4, 24, 14, 0, 0, TimeSpan.Zero);
        var t3 = new DateTimeOffset(2026, 4, 24, 16, 0, 0, TimeSpan.Zero);
        var entity = new EntityRef("agent", "rachael");

        await _kg.AddAsync(new TemporalTriple(
            new Triple(entity, "event1", new EntityRef("x", "y"), null), t1, null, t1));
        await _kg.AddAsync(new TemporalTriple(
            new Triple(entity, "event2", new EntityRef("x", "y"), null), t2, null, t2));
        await _kg.AddAsync(new TemporalTriple(
            new Triple(entity, "event3", new EntityRef("x", "y"), null), t3, null, t3));

        // Act - filter from t2 onwards
        var timeline = await _kg.TimelineAsync(entity, from: t2);

        // Assert
        timeline.Should().HaveCount(2);
        timeline.Should().AllSatisfy(e => e.At.Should().BeOnOrAfter(t2));
    }

    [Fact]
    public async Task TimelineAsync_WithToFilter_ReturnsEventsBeforeTo()
    {
        // Arrange
        var t1 = new DateTimeOffset(2026, 4, 24, 10, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2026, 4, 24, 14, 0, 0, TimeSpan.Zero);
        var t3 = new DateTimeOffset(2026, 4, 24, 16, 0, 0, TimeSpan.Zero);
        var entity = new EntityRef("agent", "deckard");

        await _kg.AddAsync(new TemporalTriple(
            new Triple(entity, "event1", new EntityRef("x", "y"), null), t1, null, t1));
        await _kg.AddAsync(new TemporalTriple(
            new Triple(entity, "event2", new EntityRef("x", "y"), null), t2, null, t2));
        await _kg.AddAsync(new TemporalTriple(
            new Triple(entity, "event3", new EntityRef("x", "y"), null), t3, null, t3));

        // Act - filter up to t2 (exclusive)
        var timeline = await _kg.TimelineAsync(entity, to: t2);

        // Assert
        timeline.Should().HaveCount(1);
        timeline[0].At.Should().BeBefore(t2);
    }

    [Fact]
    public async Task AddManyAsync_InsertsAllTriplesInTransaction()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var triples = new[]
        {
            new TemporalTriple(
                new Triple(new EntityRef("agent", "tyrell"), "worked-on", new EntityRef("project", "Core"), null),
                now, null, now),
            new TemporalTriple(
                new Triple(new EntityRef("agent", "roy"), "worked-on", new EntityRef("project", "Ai"), null),
                now, null, now),
            new TemporalTriple(
                new Triple(new EntityRef("agent", "rachael"), "worked-on", new EntityRef("project", "CLI"), null),
                now, null, now)
        };

        // Act
        var count = await _kg.AddManyAsync(triples);

        // Assert
        count.Should().Be(3);
        var totalCount = await _kg.CountAsync();
        totalCount.Should().Be(3);
    }

    [Fact]
    public async Task CountAsync_ReturnsZeroForEmptyGraph()
    {
        // Act
        var count = await _kg.CountAsync();

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task CountAsync_ReturnsTotalTriplesCount()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("a", "1"), "rel", new EntityRef("b", "2"), null), now, null, now));
        await _kg.AddAsync(new TemporalTriple(
            new Triple(new EntityRef("c", "3"), "rel", new EntityRef("d", "4"), null), now, null, now));

        // Act
        var count = await _kg.CountAsync();

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task TripleWithProperties_RoundTrips()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var props = new Dictionary<string, object?>
        {
            ["confidence"] = 0.95,
            ["source"] = "session-abc-123",
            ["tags"] = new[] { "important", "reviewed" }
        };
        var triple = new Triple(
            new EntityRef("agent", "tyrell"),
            "authored",
            new EntityRef("decision", "tech-stack"),
            props
        );
        var temporal = new TemporalTriple(triple, now, null, now);

        // Act
        await _kg.AddAsync(temporal);
        var results = await _kg.QueryAsync(new TriplePattern(null, null, null));

        // Assert
        results.Should().HaveCount(1);
        results[0].Triple.Properties.Should().NotBeNull();
        results[0].Triple.Properties.Should().ContainKey("confidence");
        results[0].Triple.Properties.Should().ContainKey("source");
    }
}
