using System.Net;
using System.Text.RegularExpressions;
using ChatGbtApp;
using ChatGbtApp.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace ChatGgtApp.Crawler;

public class JobsCrawler
{
    private readonly JobStorage _jobStorage;
    private readonly Chromium _chromium;
    private readonly OpenAiApi _openAiApi;
    private readonly ILogger<JobsCrawler> _logger;
    private readonly GptKeyValueParser _gptKeyValueParser;
    private readonly string _prompt;

    public JobsCrawler(JobStorage jobStorage, Chromium chromium, OpenAiApi openAiApi, ILogger<JobsCrawler> logger,
        GptKeyValueParser gptKeyValueParser)
    {
        _jobStorage = jobStorage;
        _chromium = chromium;
        _openAiApi = openAiApi;
        _logger = logger;
        _gptKeyValueParser = gptKeyValueParser;

        var solutionRoot = SolutionDirectory.FindRepoRoot(Directory.GetCurrentDirectory());
        var promptTemplate = File.ReadAllText(Path.Combine(solutionRoot, "data/prompt.txt"));
        var cv = File.ReadAllText(Path.Combine(solutionRoot, "data/cv.txt"));

        _prompt = promptTemplate
            .Replace("{{CV}}", cv);
    }

    public async Task CrawlJobs(string urls)
    {
        int i = 0;

        var links = urls.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var url in links)
        {
            _logger.LogInformation($"{i}: Downloading {url}");
            
            var result = await _chromium.FetchAsync(url);

            if (result.IsLoggedOut)
            {
                // 1) Run your manual login bootstrap (or show message to user)
                var email = Environment.GetEnvironmentVariable("LOOP_EMAIL");
                var pass = Environment.GetEnvironmentVariable("LOOP_PASS");
                await _chromium.BootstrapLoginAsync(email, pass); // logs in and updates auth.json

                // 2) Retry once
                result = await _chromium.FetchAsync(url);

                if (result.IsLoggedOut)
                    throw new Exception("Still logged out after login bootstrap.");
            }
            

            Console.WriteLine(result.Content);
            // return;
            _logger.LogInformation($"{i}: Asking ChatGBT {url}");
            var input = _prompt.Replace("{{JOB DESCROPTION}}", result.Content ?? string.Empty);
            var message = await _openAiApi.AskAsync(input);

            var values= _gptKeyValueParser.ParseOrNull(message);
            if (values != null)
            {
                _logger.LogError($"{i}: values are null for message {message}");
                continue;
            }

            _logger.LogInformation($"{i}: Storing result {url}");
            await _jobStorage.Store(url, message);
        }
    }
}