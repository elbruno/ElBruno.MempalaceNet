using MemPalace.Agents.Diary;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using MemPalace.Search;
using NSubstitute;

namespace MemPalace.Tests.Agents;

public sealed class BackedByPalaceDiaryTests
{
    [Fact]
    public async Task AppendAsync_StoresEntryInBackend()
    {
        var backend = Substitute.For<IBackend>();
        var embedder = Substitute.For<IEmbedder>();
        var searchService = Substitute.For<ISearchService>();
        var collection = Substitute.For<ICollection>();

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            create: true,
            embedder: Arg.Any<IEmbedder?>(),
            ct: Arg.Any<CancellationToken>())
            .Returns(collection);

        embedder.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ReadOnlyMemory<float>> { new float[] { 0.1f, 0.2f, 0.3f } });

        var diary = new BackedByPalaceDiary(backend, embedder, searchService);
        var entry = new DiaryEntry("test-agent", DateTimeOffset.UtcNow, "user", "Hello");

        await diary.AppendAsync("test-agent", entry);

        await collection.Received(1).AddAsync(
            Arg.Is<IReadOnlyList<EmbeddedRecord>>(r => r.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecentAsync_ReturnsEmptyWhenNoEntries()
    {
        var backend = Substitute.For<IBackend>();
        var embedder = Substitute.For<IEmbedder>();
        var searchService = Substitute.For<ISearchService>();

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            create: false,
            embedder: Arg.Any<IEmbedder?>(),
            ct: Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromException<ICollection>(new Exception("Not found")));

        var diary = new BackedByPalaceDiary(backend, embedder, searchService);
        var entries = await diary.RecentAsync("test-agent");

        Assert.Empty(entries);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmptyWhenNoResults()
    {
        var backend = Substitute.For<IBackend>();
        var embedder = Substitute.For<IEmbedder>();
        var searchService = Substitute.For<ISearchService>();

        searchService.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<SearchOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<SearchHit>>(new Exception("Not found")));

        var diary = new BackedByPalaceDiary(backend, embedder, searchService);
        var entries = await diary.SearchAsync("test-agent", "test query");

        Assert.Empty(entries);
    }
}
