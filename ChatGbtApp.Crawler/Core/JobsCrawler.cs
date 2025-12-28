using ChatGgtApp.Crawler.Progress;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatGgtApp.Crawler.Core;

/// <summary>
/// Orchestrates the crawling and processing of multiple job postings.
/// Handles parallel processing and progress tracking.
/// </summary>
public class JobsCrawler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly JobProcessingProgress _progress;
    private readonly ILogger<JobsCrawler> _logger;
    private readonly int _maxParallelism;

    public JobsCrawler(
        IServiceScopeFactory scopeFactory,
        JobProcessingProgress progress,
        ILogger<JobsCrawler> logger,
        int maxParallelism = 1)
    {
        _scopeFactory = scopeFactory;
        _progress = progress;
        _logger = logger;
        _maxParallelism = maxParallelism;
    }
    public async Task CrawlJobsAsync(List<JobUrl> jobUrls)
    {
        if (jobUrls.Count == 0)
        {
            _logger.LogWarning("No URLs provided to crawl");
            return;
        }

        _progress.Reset(jobUrls.Count);
        _logger.LogInformation("Starting crawl of {Count} job(s) with parallelism {Parallelism}", 
            jobUrls.Count, _maxParallelism);

        await Parallel.ForEachAsync(
            jobUrls,
            new ParallelOptions { MaxDegreeOfParallelism = _maxParallelism },
            async (jobUrl, ct) => await ProcessSingleJobAsync(jobUrl));

        _progress.PrintSummary();
    }

    private async Task ProcessSingleJobAsync(JobUrl jobUrl)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<JobProcessor>();

            await processor.ProcessJobAsync(jobUrl);
            _progress.LogProgress();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "[{jobUrl}] Unexpected error during processing", jobUrl);
        }
    }
}