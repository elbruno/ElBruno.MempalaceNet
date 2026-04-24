using System.Runtime.CompilerServices;
using FluentAssertions;
using MemPalace.Mining;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using NSubstitute;

namespace MemPalace.Tests.Mining;

public sealed class MiningPipelineTests
{
    [Fact]
    public async Task RunAsync_ProcessesItemsInBatches()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var embedder = Substitute.For<IEmbedder>();
        var collection = Substitute.For<ICollection>();
        
        embedder.ModelIdentity.Returns("test-model");
        embedder.Dimensions.Returns(128);
        embedder.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var texts = call.Arg<IReadOnlyList<string>>();
                var result = texts.Select(_ => new ReadOnlyMemory<float>(new float[128])).ToList();
                return ValueTask.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(result);
            });

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>()
        ).Returns(collection);

        var items = new[]
        {
            new MinedItem("id1", "content1", new Dictionary<string, object?>()),
            new MinedItem("id2", "content2", new Dictionary<string, object?>()),
            new MinedItem("id3", "content3", new Dictionary<string, object?>())
        };

        var miner = new TestMiner(items);
        var ctx = new MinerContext("/test", null, new Dictionary<string, string?> { ["batch_size"] = "2" });
        var pipeline = new MiningPipeline();

        // Act
        var report = await pipeline.RunAsync(miner, ctx, backend, embedder, "test-collection");

        // Assert
        report.ItemsMined.Should().Be(3);
        report.Batches.Should().Be(2); // 2 items + 1 item
        report.Embedded.Should().Be(3);
        report.Upserted.Should().Be(3);
        report.Skipped.Should().Be(0);
        report.Errors.Should().BeEmpty();

        await collection.Received(2).UpsertAsync(Arg.Any<IReadOnlyList<EmbeddedRecord>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_DeduplicatesWithinRun()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var embedder = Substitute.For<IEmbedder>();
        var collection = Substitute.For<ICollection>();
        
        embedder.ModelIdentity.Returns("test-model");
        embedder.Dimensions.Returns(128);
        embedder.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var texts = call.Arg<IReadOnlyList<string>>();
                var result = texts.Select(_ => new ReadOnlyMemory<float>(new float[128])).ToList();
                return ValueTask.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(result);
            });

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>()
        ).Returns(collection);

        var items = new[]
        {
            new MinedItem("id1", "content1", new Dictionary<string, object?>()),
            new MinedItem("id1", "duplicate", new Dictionary<string, object?>()),
            new MinedItem("id2", "content2", new Dictionary<string, object?>())
        };

        var miner = new TestMiner(items);
        var ctx = new MinerContext("/test", null, new Dictionary<string, string?>());
        var pipeline = new MiningPipeline();

        // Act
        var report = await pipeline.RunAsync(miner, ctx, backend, embedder, "test-collection");

        // Assert
        report.ItemsMined.Should().Be(3);
        report.Upserted.Should().Be(2);
        report.Skipped.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_HandlesEmbedderErrors()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var embedder = Substitute.For<IEmbedder>();
        var collection = Substitute.For<ICollection>();
        
        embedder.ModelIdentity.Returns("test-model");
        embedder.Dimensions.Returns(128);
        embedder.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<IReadOnlyList<ReadOnlyMemory<float>>>>(_ => throw new Exception("Embedding failed"));

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>()
        ).Returns(collection);

        var items = new[]
        {
            new MinedItem("id1", "content1", new Dictionary<string, object?>())
        };

        var miner = new TestMiner(items);
        var ctx = new MinerContext("/test", null, new Dictionary<string, string?>());
        var pipeline = new MiningPipeline();

        // Act
        var report = await pipeline.RunAsync(miner, ctx, backend, embedder, "test-collection");

        // Assert
        report.ItemsMined.Should().Be(1);
        report.Errors.Should().NotBeEmpty();
        report.Errors[0].Should().Contain("Batch processing error");
    }

    [Fact]
    public async Task RunAsync_ReportsElapsedTime()
    {
        // Arrange
        var backend = Substitute.For<IBackend>();
        var embedder = Substitute.For<IEmbedder>();
        var collection = Substitute.For<ICollection>();
        
        embedder.ModelIdentity.Returns("test-model");
        embedder.Dimensions.Returns(128);
        embedder.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var texts = call.Arg<IReadOnlyList<string>>();
                var result = texts.Select(_ => new ReadOnlyMemory<float>(new float[128])).ToList();
                return ValueTask.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(result);
            });

        backend.GetCollectionAsync(
            Arg.Any<PalaceRef>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IEmbedder?>(),
            Arg.Any<CancellationToken>()
        ).Returns(collection);

        var items = new[] { new MinedItem("id1", "content", new Dictionary<string, object?>()) };
        var miner = new TestMiner(items);
        var ctx = new MinerContext("/test", null, new Dictionary<string, string?>());
        var pipeline = new MiningPipeline();

        // Act
        var report = await pipeline.RunAsync(miner, ctx, backend, embedder, "test-collection");

        // Assert
        report.Elapsed.Should().BeGreaterThan(TimeSpan.Zero);
    }

    private sealed class TestMiner : IMiner
    {
        private readonly MinedItem[] _items;

        public TestMiner(MinedItem[] items)
        {
            _items = items;
        }

        public string Name => "test";

        public async IAsyncEnumerable<MinedItem> MineAsync(
            MinerContext ctx,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            foreach (var item in _items)
            {
                yield return item;
            }
            await Task.CompletedTask;
        }
    }
}
