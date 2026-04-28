using Spectre.Console;

namespace MemPalace.Cli.Output;

/// <summary>
/// Utilities for displaying progress bars in long-running operations.
/// </summary>
internal static class ProgressDisplay
{
    /// <summary>
    /// Executes a mining operation with progress tracking.
    /// </summary>
    public static async Task<TResult> WithMiningProgress<TResult>(
        string description,
        int totalItems,
        Func<IProgress<MiningProgress>, Task<TResult>> operation)
    {
        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            // Non-TTY fallback: log-style output
            AnsiConsole.MarkupLine($"[yellow]Starting:[/] {description}");
            var result = await operation(new LogProgress());
            AnsiConsole.MarkupLine($"[green]✓[/] Completed {description}");
            return result;
        }

        return await AnsiConsole.Progress()
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask(description, maxValue: totalItems);
                var progress = new SpectreProgress(task);
                return await operation(progress);
            });
    }

    /// <summary>
    /// Executes a search reranking operation with progress tracking.
    /// </summary>
    public static async Task<TResult> WithRerankProgress<TResult>(
        int totalResults,
        Func<IProgress<RerankProgress>, Task<TResult>> operation)
    {
        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            // Non-TTY fallback: log-style output
            AnsiConsole.MarkupLine($"[yellow]Reranking {totalResults} results...[/]");
            var result = await operation(new LogRerankProgress());
            AnsiConsole.MarkupLine($"[green]✓[/] Reranking complete");
            return result;
        }

        return await AnsiConsole.Progress()
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[green]Reranking {totalResults} results[/]", maxValue: totalResults);
                var progress = new SpectreRerankProgress(task);
                return await operation(progress);
            });
    }

    /// <summary>
    /// Executes a generic operation with progress tracking.
    /// </summary>
    public static async Task<TResult> WithProgress<TResult>(
        string description,
        int totalItems,
        Func<IProgress<ProgressInfo>, Task<TResult>> operation)
    {
        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            // Non-TTY fallback
            AnsiConsole.MarkupLine($"[yellow]Starting:[/] {description}");
            var result = await operation(new LogGenericProgress());
            AnsiConsole.MarkupLine($"[green]✓[/] {description} complete");
            return result;
        }

        return await AnsiConsole.Progress()
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask(description, maxValue: totalItems);
                var progress = new SpectreGenericProgress(task);
                return await operation(progress);
            });
    }

    // Progress data classes
    public record MiningProgress(int ProcessedFiles, int TotalFiles, string? CurrentFile);
    public record RerankProgress(int ProcessedResults, int TotalResults);
    public record ProgressInfo(int Current, int Total, string? Status = null);

    // Spectre.Console progress adapters
    private sealed class SpectreProgress : IProgress<MiningProgress>
    {
        private readonly ProgressTask _task;

        public SpectreProgress(ProgressTask task) => _task = task;

        public void Report(MiningProgress value)
        {
            _task.Value = value.ProcessedFiles;
            if (value.CurrentFile != null)
            {
                _task.Description = $"[green]Mining:[/] {value.CurrentFile}";
            }
        }
    }

    private sealed class SpectreRerankProgress : IProgress<RerankProgress>
    {
        private readonly ProgressTask _task;

        public SpectreRerankProgress(ProgressTask task) => _task = task;

        public void Report(RerankProgress value)
        {
            _task.Value = value.ProcessedResults;
        }
    }

    private sealed class SpectreGenericProgress : IProgress<ProgressInfo>
    {
        private readonly ProgressTask _task;

        public SpectreGenericProgress(ProgressTask task) => _task = task;

        public void Report(ProgressInfo value)
        {
            _task.Value = value.Current;
            if (value.Status != null)
            {
                _task.Description = value.Status;
            }
        }
    }

    // Log-style progress for non-TTY terminals
    private sealed class LogProgress : IProgress<MiningProgress>
    {
        private int _lastReported = -1;

        public void Report(MiningProgress value)
        {
            var percentComplete = (int)((value.ProcessedFiles / (double)value.TotalFiles) * 100);
            if (percentComplete != _lastReported && percentComplete % 10 == 0)
            {
                AnsiConsole.MarkupLine($"[dim]Progress: {percentComplete}% ({value.ProcessedFiles}/{value.TotalFiles} files)[/]");
                _lastReported = percentComplete;
            }
        }
    }

    private sealed class LogRerankProgress : IProgress<RerankProgress>
    {
        public void Report(RerankProgress value)
        {
            AnsiConsole.MarkupLine($"[dim]Reranked: {value.ProcessedResults}/{value.TotalResults}[/]");
        }
    }

    private sealed class LogGenericProgress : IProgress<ProgressInfo>
    {
        private int _lastReported = -1;

        public void Report(ProgressInfo value)
        {
            var percentComplete = (int)((value.Current / (double)value.Total) * 100);
            if (percentComplete != _lastReported && percentComplete % 10 == 0)
            {
                AnsiConsole.MarkupLine($"[dim]Progress: {percentComplete}% ({value.Current}/{value.Total})[/]");
                _lastReported = percentComplete;
            }
        }
    }
}
