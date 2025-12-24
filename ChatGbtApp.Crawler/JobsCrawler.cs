using ChatGbtApp;
using Microsoft.Extensions.Logging;

namespace ChatGgtApp.Crawler;

public class JobsCrawler
{
    private readonly JobStorage _jobStorage;
    private readonly Chromium _chromium;
    private readonly OpenAiApi _openAiApi;
    private readonly ILogger<JobsCrawler> _logger;
    private readonly GptKeyValueParser _gptKeyValueParser;
    private readonly JobProcessingProgress _progress;
    private readonly string _prompt;

    public JobsCrawler(JobStorage jobStorage, Chromium chromium, OpenAiApi openAiApi, ILogger<JobsCrawler> logger,
        GptKeyValueParser gptKeyValueParser, JobProcessingProgress progress)
    {
        _jobStorage = jobStorage;
        _chromium = chromium;
        _openAiApi = openAiApi;
        _logger = logger;
        _gptKeyValueParser = gptKeyValueParser;
        _progress = progress;

        var solutionRoot = SolutionDirectory.FindRepoRoot();
        var promptTemplate = File.ReadAllText(Path.Combine(solutionRoot, "data/prompt.txt"));
        var cv = File.ReadAllText(Path.Combine(solutionRoot, "data/cv.txt"));

        _prompt = promptTemplate
            .Replace("{{CV}}", cv);
    }

    public async Task CrawlJobs(string urls)
    {
        var links = urls.Split(',', StringSplitOptions.RemoveEmptyEntries);
        _progress.Reset(links.Length);
        
        //foreach (var url in links)
        await Parallel.ForEachAsync(
            links,
            new ParallelOptions { MaxDegreeOfParallelism = 4 },
            async (url, ct) =>
            {
                try
                {
                    if (await _jobStorage.IsDuplicate(url))
                    {
                        _progress.LogProgress();
                        return;
                    }

                    _logger.LogInformation($"[{url}] Starting job processing...");

                    var jobPage = await _chromium.FetchAsync(url);

                    if (jobPage.IsLoggedOut)
                    {
                        // 1) Run your manual login bootstrap (or show message to user)
                        var email = Environment.GetEnvironmentVariable("LOOP_EMAIL");
                        var pass = Environment.GetEnvironmentVariable("LOOP_PASS");
                        await _chromium.BootstrapLoginAsync(email, pass); // logs in and updates auth.json

                        // 2) Retry once
                        jobPage = await _chromium.FetchAsync(url);

                        if (jobPage.IsLoggedOut)
                            throw new Exception("Still logged out after login bootstrap.");
                    }

                    var jobDescription = jobPage.Content ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(jobDescription))
                    {
                        _progress.RecordEmpty();
                        _logger.LogWarning($"[{url}] Empty content detected; skipping store.");
                        return;
                    }

                    _logger.LogInformation($"[{url}] Requesting analysis from ChatGPT...");
                    var input = _prompt.Replace("{{JOB DESCROPTION}}", jobDescription);
                    var message = await _openAiApi.AskAsync(input);

                    var values = _gptKeyValueParser.ParseOrNull(message);
                    if (values == null)
                    {
                        _logger.LogError($"[{url}] Failed to parse ChatGPT response; skipping.");
                        return;
                    }

                    _logger.LogInformation($"[{url}] Storing parsed results...");
                    await _jobStorage.Store(url, jobDescription, message, values);

                    _logger.LogInformation(
                        $"[{url}] ChatGBT MatchScore: [{values.MatchScore}], Remote: {values.Remote} Summary: {values.Summary}");

                    _progress.LogProgress();
                }
                catch (Exception e)
                {
                    _logger.LogCritical($"[{url}] Error processing: {e.Message}");
                    Console.WriteLine(e);
                }
            });
        _progress.PrintSummary();
    }
}