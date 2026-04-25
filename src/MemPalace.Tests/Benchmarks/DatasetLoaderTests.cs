using FluentAssertions;
using MemPalace.Benchmarks.Core;
using System.Text.Json;

namespace MemPalace.Tests.Benchmarks;

public sealed class DatasetLoaderTests
{
    [Fact]
    public async Task LoadAsync_ValidJsonl_ReturnsItems()
    {
        // Create a temp JSONL file
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, """
                {"id": "1", "question": "What is 2+2?", "expected_answer": "4", "relevant_memory_ids": ["m1"], "metadata": {"difficulty": "easy"}}
                {"id": "2", "question": "What is AI?", "expected_answer": "Artificial Intelligence", "relevant_memory_ids": ["m2", "m3"], "metadata": {"difficulty": "medium"}}
                """);

            var items = await DatasetLoader.LoadAsync(tempFile).ToListAsync();

            items.Should().HaveCount(2);
            
            items[0].Id.Should().Be("1");
            items[0].Question.Should().Be("What is 2+2?");
            items[0].ExpectedAnswer.Should().Be("4");
            items[0].RelevantMemoryIds.Should().BeEquivalentTo(new[] { "m1" });
            items[0].Metadata.Should().ContainKey("difficulty");

            items[1].Id.Should().Be("2");
            items[1].RelevantMemoryIds.Should().BeEquivalentTo(new[] { "m2", "m3" });
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadAsync_MaxItems_LimitsResults()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, """
                {"id": "1", "question": "Q1", "expected_answer": "A1", "relevant_memory_ids": ["m1"], "metadata": {}}
                {"id": "2", "question": "Q2", "expected_answer": "A2", "relevant_memory_ids": ["m2"], "metadata": {}}
                {"id": "3", "question": "Q3", "expected_answer": "A3", "relevant_memory_ids": ["m3"], "metadata": {}}
                """);

            var items = await DatasetLoader.LoadAsync(tempFile, maxItems: 2).ToListAsync();

            items.Should().HaveCount(2);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadAsync_EmptyLines_SkipsThem()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, """
                {"id": "1", "question": "Q1", "expected_answer": "A1", "relevant_memory_ids": ["m1"], "metadata": {}}

                {"id": "2", "question": "Q2", "expected_answer": "A2", "relevant_memory_ids": ["m2"], "metadata": {}}
                """);

            var items = await DatasetLoader.LoadAsync(tempFile).ToListAsync();

            items.Should().HaveCount(2);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadAsync_FileNotFound_ThrowsFileNotFoundException()
    {
        var act = () => DatasetLoader.LoadAsync("nonexistent.jsonl").ToListAsync().AsTask();

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task LoadAsync_UpstreamLongMemEvalJsonArray_MapsFreshHaystackDataset()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, """
                [
                  {
                    "question_id": "e47becba",
                    "question": "What degree did I graduate with?",
                    "answer": "Business Administration",
                    "answer_session_ids": ["answer_280352e9"],
                    "haystack_session_ids": ["answer_280352e9", "distractor"],
                    "haystack_dates": ["2024-01-10", "2024-01-12"],
                    "haystack_sessions": [
                      [
                        { "role": "user", "content": "I graduated with a Business Administration degree." },
                        { "role": "assistant", "content": "That sounds useful." }
                      ],
                      [
                        { "role": "user", "content": "I bought a new lamp today." }
                      ]
                    ]
                  }
                ]
                """);

            var items = await DatasetLoader.LoadAsync(tempFile).ToListAsync();

            items.Should().HaveCount(1);
            items[0].Id.Should().Be("e47becba");
            items[0].ExpectedAnswer.Should().Be("Business Administration");
            items[0].RelevantMemoryIds.Should().Equal("answer_280352e9");
            items[0].Metadata["source_format"].Should().Be("longmemeval-upstream");
            items[0].CorpusDocuments.Should().NotBeNull();
            items[0].CorpusDocuments.Should().HaveCount(2);
            items[0].CorpusDocuments![0].Id.Should().Be("answer_280352e9");
            items[0].CorpusDocuments![0].Document.Should().Contain("Business Administration degree");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
