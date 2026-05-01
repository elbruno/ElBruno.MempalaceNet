using FluentAssertions;
using MemPalace.Backends.Sqlite;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using MemPalace.KnowledgeGraph;
using MemPalace.Tests.Backends;

namespace MemPalace.E2E.Tests;

/// <summary>
/// E2E journey tests covering complete user workflows.
/// Tests: Init → Store → Search → WakeUp → Knowledge Graph operations.
/// </summary>
public sealed class FullJourneyTests : IDisposable
{
    private readonly string _testDir;

    public FullJourneyTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"mempalace-e2e-journey-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, recursive: true); }
            catch { /* best effort cleanup */ }
        }
    }

    [Fact]
    public async Task Journey_CompleteWorkflow_InitToKnowledgeGraph_Success()
    {
        // Phase 1: Initialize palace
        var palaceRef = new PalaceRef("journey-palace");
        var embedder = new FakeEmbedder();
        using var backend = new SqliteBackend(_testDir);

        var collection = await backend.GetCollectionAsync(
            palaceRef,
            "default",
            create: true,
            embedder: embedder);

        // Phase 2: Store memories (mining simulation)
        var documents = new[]
        {
            "Alice works on authentication module",
            "Bob works on payment processing",
            "Charlie works on frontend UI",
            "Diana optimizes database queries",
            "Eve designs REST APIs"
        };

        var records = new List<EmbeddedRecord>();
        for (int i = 0; i < documents.Length; i++)
        {
            var embedding = (await embedder.EmbedAsync(new[] { documents[i] }))[0].ToArray();
            records.Add(new EmbeddedRecord(
                Id: $"mem-{i}",
                Document: documents[i],
                Metadata: new Dictionary<string, object?>
                {
                    { "wing", "team" },
                    { "source", "onboarding" }
                },
                Embedding: embedding
            ));
        }

        await collection.AddAsync(records);

        // Phase 3: Semantic search
        var queryEmbeddings = await embedder.EmbedAsync(new[] { "authentication security" });
        var searchResults = await collection.QueryAsync(queryEmbeddings, nResults: 3);

        searchResults.Ids.Should().NotBeEmpty();
        searchResults.Documents.Should().Contain(d => d.Contains("authentication"));

        // Phase 4: Wake-up (recent memories)
        var wakeUpResults = await collection.GetAsync(
            limit: 10,
            include: IncludeFields.Documents | IncludeFields.Metadatas);

        wakeUpResults.Documents.Should().HaveCount(documents.Length);

        // Phase 5: Knowledge Graph operations
        var kg = new SqliteKnowledgeGraph(Path.Combine(_testDir, "kg.db"));
        
        await kg.AddEntityAsync("alice", "person", new { name = "Alice", role = "engineer" });
        await kg.AddEntityAsync("auth-module", "project");
        await kg.AddRelationshipAsync("alice", "auth-module", "works_on");

        var relationships = await kg.QueryAsync("alice", "works_on");
        relationships.Should().HaveCount(1);
        relationships[0].Object.Should().Be("auth-module");

        await kg.DisposeAsync();
        await collection.DisposeAsync();
        await backend.DisposeAsync();
    }

    [Fact]
    public async Task Journey_MultiWingWorkflow_SeparateCollections_Success()
    {
        // Phase 1: Initialize
        var palaceRef = new PalaceRef("multi-wing-palace");
        var embedder = new FakeEmbedder();
        using var backend = new SqliteBackend(_testDir);

        // Phase 2: Create separate wings (collections)
        var workCollection = await backend.GetCollectionAsync(palaceRef, "work", create: true, embedder: embedder);
        var personalCollection = await backend.GetCollectionAsync(palaceRef, "personal", create: true, embedder: embedder);

        // Phase 3: Store memories in each wing
        var workDocs = new[] { "Team standup notes", "Sprint planning agenda" };
        var personalDocs = new[] { "Grocery list", "Book recommendations" };

        await StoreDocumentsAsync(workCollection, workDocs, embedder);
        await StoreDocumentsAsync(personalCollection, personalDocs, embedder);

        // Phase 4: Verify isolation
        var workResults = await workCollection.GetAsync(limit: 10, include: IncludeFields.Documents);
        var personalResults = await personalCollection.GetAsync(limit: 10, include: IncludeFields.Documents);

        workResults.Documents.Should().HaveCount(2);
        personalResults.Documents.Should().HaveCount(2);
        workResults.Documents.Should().NotIntersectWith(personalResults.Documents);

        await workCollection.DisposeAsync();
        await personalCollection.DisposeAsync();
        await backend.DisposeAsync();
    }

    private async Task StoreDocumentsAsync(ICollection collection, string[] documents, IEmbedder embedder)
    {
        var records = new List<EmbeddedRecord>();
        for (int i = 0; i < documents.Length; i++)
        {
            var embedding = (await embedder.EmbedAsync(new[] { documents[i] }))[0].ToArray();
            records.Add(new EmbeddedRecord(
                Id: Guid.NewGuid().ToString(),
                Document: documents[i],
                Metadata: new Dictionary<string, object?> { { "index", i } },
                Embedding: embedding
            ));
        }
        await collection.AddAsync(records);
    }
}
