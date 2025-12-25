using ChatGbtApp;
using ChatGgtApp.Crawler.Browser;
using ChatGgtApp.Crawler.Parsers;
using ChatGgtApp.Crawler.Progress;
using ChatGgtApp.Crawler.Storage;
using Microsoft.Extensions.Logging;

namespace ChatGgtApp.Crawler.Core;

public class JobsCrawler(
    JobStorage jobStorage,
    Chromium chromium,
    OpenAiApi openAiApi,
    ILogger<JobsCrawler> logger,
    GptKeyValueParser gptKeyValueParser,
    JobProcessingProgress progress,
    Prompt prompt)
{
    public async Task CrawlJobs(string urls)
    {
        var links = urls.Split(',', StringSplitOptions.RemoveEmptyEntries);
        progress.Reset(links.Length);
        
        //foreach (var url in links)
        await Parallel.ForEachAsync(
            links,
            new ParallelOptions { MaxDegreeOfParallelism = 4 },
            async (url, ct) =>
            {
                try
                {
                    if (jobStorage.IsDuplicate(url))
                    {
                        progress.LogProgress();
                        return;
                    }

                    logger.LogInformation($"[{url}] Starting job processing...");

                    var jobPage = await chromium.FetchAsync(url);

                    if (jobPage.IsLoggedOut)
                    {
                        // 1) Run your manual login bootstrap (or show message to user)
                        var email = Environment.GetEnvironmentVariable("LOOP_EMAIL");
                        var pass = Environment.GetEnvironmentVariable("LOOP_PASS");
                        await chromium.BootstrapLoginAsync(email, pass); // logs in and updates auth.json

                        // 2) Retry once
                        jobPage = await chromium.FetchAsync(url);

                        if (jobPage.IsLoggedOut)
                            throw new Exception("Still logged out after login bootstrap.");
                    }

                    var jobDescription = jobPage.Content ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(jobDescription))
                    {
                        progress.RecordEmpty();
                        logger.LogWarning($"[{url}] Empty content detected; skipping store.");
                        return;
                    }

                    logger.LogInformation($"[{url}] Requesting analysis from ChatGPT...");
                    var input = prompt.GetPrompt(jobDescription);
                    var message = await openAiApi.AskAsync(input);

                    var values = gptKeyValueParser.ParseOrNull(message);
                    if (values == null)
                    {
                        logger.LogError($"[{url}] Failed to parse ChatGPT response; skipping.");
                        return;
                    }

                    logger.LogInformation($"[{url}] Storing parsed results...");
                    jobStorage.Store(url, jobDescription, message, values);

                    logger.LogInformation(
                        $"[{url}] ChatGBT MatchScore: [{values.MatchScore}], Remote: {values.Remote} Summary: {values.Summary}");

                    progress.LogProgress();
                }
                catch (Exception e)
                {
                    logger.LogCritical($"[{url}] Error processing: {e.Message}");
                    Console.WriteLine(e);
                }
            });
        progress.PrintSummary();
    }
}