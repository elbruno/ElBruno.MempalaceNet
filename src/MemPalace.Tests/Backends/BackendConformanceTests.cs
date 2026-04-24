using MemPalace.Core.Backends;
using MemPalace.Core.Errors;
using MemPalace.Core.Model;

namespace MemPalace.Tests.Backends;

/// <summary>
/// Abstract conformance test suite for IBackend implementations.
/// Subclass and override CreateBackend() to test your backend.
/// </summary>
public abstract class BackendConformanceTests : IAsyncLifetime
{
    protected IBackend Backend { get; private set; } = null!;
    protected FakeEmbedder Embedder { get; private set; } = null!;
    protected PalaceRef TestPalace { get; private set; } = null!;

    protected abstract IBackend CreateBackend();

    public async Task InitializeAsync()
    {
        Backend = CreateBackend();
        Embedder = new FakeEmbedder();
        TestPalace = new PalaceRef($"test-palace-{Guid.NewGuid()}");
    }

    public async Task DisposeAsync()
    {
        if (Backend != null)
            await Backend.DisposeAsync();
    }

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        var health = await Backend.HealthAsync();
        Assert.True(health.Ok);
    }

    [Fact]
    public async Task GetCollection_CreateFalse_ThrowsPalaceNotFound()
    {
        await Assert.ThrowsAsync<PalaceNotFoundException>(async () =>
            await Backend.GetCollectionAsync(TestPalace, "nonexistent", create: false));
    }

    [Fact]
    public async Task GetCollection_Create_CreatesCollection()
    {
        var collection = await Backend.GetCollectionAsync(TestPalace, "test-collection", create: true, embedder: Embedder);
        
        Assert.Equal("test-collection", collection.Name);
        Assert.Equal(Embedder.Dimensions, collection.Dimensions);
        Assert.Equal(Embedder.ModelIdentity, collection.EmbedderIdentity);
    }

    [Fact]
    public async Task AddAndGet_StoresAndRetrievesRecords()
    {
        var collection = await Backend.GetCollectionAsync(TestPalace, "test-add", create: true, embedder: Embedder);
        
        var embeddings = await Embedder.EmbedAsync(new[] { "hello world", "test document" });
        var records = new[]
        {
            new EmbeddedRecord("id1", "hello world", new Dictionary<string, object?> { ["tag"] = "greeting" }, embeddings[0]),
            new EmbeddedRecord("id2", "test document", new Dictionary<string, object?> { ["tag"] = "test" }, embeddings[1])
        };

        await collection.AddAsync(records);
        
        var result = await collection.GetAsync(ids: new[] { "id1", "id2" });
        Assert.Equal(2, result.Ids.Count);
        Assert.Contains("id1", result.Ids);
        Assert.Contains("id2", result.Ids);
        Assert.Contains("hello world", result.Documents);
    }

    [Fact]
    public async Task Add_DuplicateId_Throws()
    {
        var collection = await Backend.GetCollectionAsync(TestPalace, "test-dup", create: true, embedder: Embedder);
        
        var embeddings = await Embedder.EmbedAsync(new[] { "doc1" });
        var record = new EmbeddedRecord("id1", "doc1", new Dictionary<string, object?>(), embeddings[0]);

        await collection.AddAsync(new[] { record });
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await collection.AddAsync(new[] { record }));
    }

    [Fact]
    public async Task Upsert_IdempotentOperation()
    {
        var collection = await Backend.GetCollectionAsync(TestPalace, "test-upsert", create: true, embedder: Embedder);
        
        var embeddings = await Embedder.EmbedAsync(new[] { "doc1", "doc1 updated" });
        var record1 = new EmbeddedRecord("id1", "doc1", new Dictionary<string, object?> { ["v"] = 1 }, embeddings[0]);
        var record2 = new EmbeddedRecord("id1", "doc1 updated", new Dictionary<string, object?> { ["v"] = 2 }, embeddings[1]);

        await collection.UpsertAsync(new[] { record1 });
        await collection.UpsertAsync(new[] { record2 });
        
        var result = await collection.GetAsync(ids: new[] { "id1" });
        Assert.Single(result.Ids);
        Assert.Contains("doc1 updated", result.Documents);
    }

    [Fact]
    public async Task Query_ReturnsTopK()
    {
        var collection = await Backend.GetCollectionAsync(TestPalace, "test-query", create: true, embedder: Embedder);
        
        var texts = new[] { "apple", "banana", "cherry", "apricot" };
        var embeddings = await Embedder.EmbedAsync(texts);
        var records = texts.Select((text, i) => new EmbeddedRecord($"id{i}", text, new Dictionary<string, object?>(), embeddings[i])).ToArray();

        await collection.AddAsync(records);
        
        var queryEmbeddings = await Embedder.EmbedAsync(new[] { "apple" });
        var result = await collection.QueryAsync(queryEmbeddings, nResults: 2);

        Assert.Single(result.Ids);
        Assert.Equal(2, result.Ids[0].Count);
        Assert.Contains("id0", result.Ids[0]);
    }

    [Fact]
    public async Task Query_OrdersByDistance()
    {
        var collection = await Backend.GetCollectionAsync(TestPalace, "test-order", create: true, embedder: Embedder);
        
        var texts = new[] { "cat", "dog", "elephant" };
        var embeddings = await Embedder.EmbedAsync(texts);
        var records = texts.Select((text, i) => new EmbeddedRecord($"id{i}", text, new Dictionary<string, object?>(), embeddings[i])).ToArray();

        await collection.AddAsync(records);
        
        var queryEmbeddings = await Embedder.EmbedAsync(new[] { "cat" });
        var result = await collection.QueryAsync(queryEmbeddings, nResults: 3);

        Assert.Equal("id0", result.Ids[0][0]); // "cat" should be first
    }

    [Fact]
    public async Task Filter_EqOperator()
    {
        var collection = await Backend.GetCollectionAsync(TestPalace, "test-filter-eq", create: true, embedder: Embedder);
        
        var embeddings = await Embedder.EmbedAsync(new[] { "doc1", "doc2" });
        var records = new[]
        {
            new EmbeddedRecord("id1", "doc1", new Dictionary<string, object?> { ["category"] = "A" }, embeddings[0]),
            new EmbeddedRecord("id2", "doc2", new Dictionary<string, object?> { ["category"] = "B" }, embeddings[1])
        };

        await collection.AddAsync(records);
        
        var result = await collection.GetAsync(where: new Eq("category", "A"));
        Assert.Single(result.Ids);
        Assert.Contains("id1", result.Ids);
    }

    [Fact]
    public async Task Filter_AndOperator()
    {
        var collection = await Backend.GetCollectionAsync(TestPalace, "test-filter-and", create: true, embedder: Embedder);
        
        var embeddings = await Embedder.EmbedAsync(new[] { "doc1", "doc2", "doc3" });
        var records = new[]
        {
            new EmbeddedRecord("id1", "doc1", new Dictionary<string, object?> { ["category"] = "A", ["priority"] = 1 }, embeddings[0]),
            new EmbeddedRecord("id2", "doc2", new Dictionary<string, object?> { ["category"] = "A", ["priority"] = 2 }, embeddings[1]),
            new EmbeddedRecord("id3", "doc3", new Dictionary<string, object?> { ["category"] = "B", ["priority"] = 1 }, embeddings[2])
        };

        await collection.AddAsync(records);
        
        var result = await collection.GetAsync(where: new And(new WhereClause[] { new Eq("category", "A"), new Eq("priority", 1) }));
        Assert.Single(result.Ids);
        Assert.Contains("id1", result.Ids);
    }

    [Fact]
    public async Task Count_ReturnsRecordCount()
    {
        var collection = await Backend.GetCollectionAsync(TestPalace, "test-count", create: true, embedder: Embedder);
        
        var embeddings = await Embedder.EmbedAsync(new[] { "doc1", "doc2", "doc3" });
        var records = embeddings.Select((emb, i) => new EmbeddedRecord($"id{i}", $"doc{i}", new Dictionary<string, object?>(), emb)).ToArray();

        await collection.AddAsync(records);
        
        var count = await collection.CountAsync();
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task Delete_ByIds()
    {
        var collection = await Backend.GetCollectionAsync(TestPalace, "test-delete-ids", create: true, embedder: Embedder);
        
        var embeddings = await Embedder.EmbedAsync(new[] { "doc1", "doc2" });
        var records = embeddings.Select((emb, i) => new EmbeddedRecord($"id{i}", $"doc{i}", new Dictionary<string, object?>(), emb)).ToArray();

        await collection.AddAsync(records);
        await collection.DeleteAsync(ids: new[] { "id0" });
        
        var count = await collection.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Delete_ByWhere()
    {
        var collection = await Backend.GetCollectionAsync(TestPalace, "test-delete-where", create: true, embedder: Embedder);
        
        var embeddings = await Embedder.EmbedAsync(new[] { "doc1", "doc2" });
        var records = new[]
        {
            new EmbeddedRecord("id1", "doc1", new Dictionary<string, object?> { ["delete"] = true }, embeddings[0]),
            new EmbeddedRecord("id2", "doc2", new Dictionary<string, object?> { ["delete"] = false }, embeddings[1])
        };

        await collection.AddAsync(records);
        await collection.DeleteAsync(where: new Eq("delete", true));
        
        var count = await collection.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task DimensionMismatch_Throws()
    {
        var collection = await Backend.GetCollectionAsync(TestPalace, "test-dim", create: true, embedder: Embedder);
        
        var wrongEmbedding = new float[64]; // Wrong dimension
        var record = new EmbeddedRecord("id1", "doc", new Dictionary<string, object?>(), wrongEmbedding);

        await Assert.ThrowsAsync<DimensionMismatchException>(async () => await collection.AddAsync(new[] { record }));
    }

    [Fact]
    public async Task EmbedderIdentityMismatch_Throws()
    {
        var collection = await Backend.GetCollectionAsync(TestPalace, "test-embedder", create: true, embedder: Embedder);
        
        var differentEmbedder = new FakeEmbedder("different-model", Embedder.Dimensions);
        
        await Assert.ThrowsAsync<EmbedderIdentityMismatchException>(async () =>
            await Backend.GetCollectionAsync(TestPalace, "test-embedder", create: false, embedder: differentEmbedder));
    }

    [Fact]
    public async Task ListCollections_ReturnsCollectionNames()
    {
        await Backend.GetCollectionAsync(TestPalace, "collection1", create: true, embedder: Embedder);
        await Backend.GetCollectionAsync(TestPalace, "collection2", create: true, embedder: Embedder);
        
        var collections = await Backend.ListCollectionsAsync(TestPalace);
        
        Assert.Equal(2, collections.Count);
        Assert.Contains("collection1", collections);
        Assert.Contains("collection2", collections);
    }

    [Fact]
    public async Task DeleteCollection_RemovesCollection()
    {
        await Backend.GetCollectionAsync(TestPalace, "to-delete", create: true, embedder: Embedder);
        await Backend.DeleteCollectionAsync(TestPalace, "to-delete");
        
        var collections = await Backend.ListCollectionsAsync(TestPalace);
        Assert.DoesNotContain("to-delete", collections);
    }

    [Fact]
    public async Task BackendClosed_ThrowsBackendClosedException()
    {
        await Backend.DisposeAsync();
        
        await Assert.ThrowsAsync<BackendClosedException>(async () =>
            await Backend.HealthAsync());
    }
}
