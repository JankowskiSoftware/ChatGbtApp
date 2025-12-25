using ChatGgtApp.Crawler.Interfaces;
using ChatGgtApp.Crawler.Progress;
using Microsoft.Extensions.Logging;

namespace ChatGgtApp.Crawler.Core;

/// <summary>
/// Orchestrates the crawling and processing of multiple job postings.
/// Handles parallel processing and progress tracking.
/// </summary>
public class JobsCrawler
{
    private readonly IJobProcessor _jobProcessor;
    private readonly JobProcessingProgress _progress;
    private readonly ILogger<JobsCrawler> _logger;
    private readonly int _maxParallelism;

    public JobsCrawler(
        IJobProcessor jobProcessor,
        JobProcessingProgress progress,
        ILogger<JobsCrawler> logger,
        int maxParallelism = 1)
    {
        _jobProcessor = jobProcessor;
        _progress = progress;
        _logger = logger;
        _maxParallelism = maxParallelism;
    }

    /// <summary>
    /// Processes multiple job URLs in parallel.
    /// </summary>
    /// <param name="urls">Comma-separated list of job URLs to process.</param>
    public async Task CrawlJobsAsync(string urls)
    {
        var links = ParseUrls(urls);
        if (links.Count == 0)
        {
            _logger.LogWarning("No URLs provided to crawl");
            return;
        }

        _progress.Reset(links.Count);
        _logger.LogInformation("Starting crawl of {Count} job(s) with parallelism {Parallelism}", 
            links.Count, _maxParallelism);

        await Parallel.ForEachAsync(
            links,
            new ParallelOptions { MaxDegreeOfParallelism = _maxParallelism },
            async (url, ct) => await ProcessSingleJobAsync(url));

        _progress.PrintSummary();
    }

    private async Task ProcessSingleJobAsync(string url)
    {
        try
        {
            var result = await _jobProcessor.ProcessJobAsync(url);

            if (result.WasEmpty)
            {
                _progress.RecordEmpty();
            }

            _progress.LogProgress();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "[{Url}] Unexpected error during processing", url);
        }
    }

    private static List<string> ParseUrls(string urls)
    {
        return urls
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(url => url.Trim())
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .ToList();
    }
}