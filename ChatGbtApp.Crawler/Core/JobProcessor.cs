using ChatGbtApp;
using ChatGbtApp.Interfaces;
using ChatGbtApp.Repository;
using ChatGgtApp.Crawler.Browser;
using ChatGgtApp.Crawler.Parsers;
using ChatGgtApp.Crawler.Progress;
using ChatGgtApp.Crawler.Storage;
using Microsoft.Extensions.Logging;

namespace ChatGgtApp.Crawler.Core;

/// <summary>
/// Processes individual job postings by extracting content, analyzing with AI,
/// and storing results.
/// </summary>
public class JobProcessor(
    JobStorage jobStorage,
    IOpenAiApi openAiApi,
    Prompt prompt,
    ChromiumFactory chromiumFactory,
    ILogger<JobProcessor> logger,
    JobProcessingProgress progress)
{
    public async Task ProcessJobAsync(string url)
    {
        if (jobStorage.IsDuplicate(url))
        {
            progress.RecordDuplicate();
            logger.LogInformation("[{Url}] Skipping duplicate job", url);
            return;
        }

        logger.LogInformation("[{Url}] Starting job processing...", url);

        var jobDescription = await GetJobDescription(url);
        if (string.IsNullOrWhiteSpace(jobDescription))
        {
            progress.RecordEmpty();
            logger.LogWarning("[{Url}] Empty content detected; skipping store.", url);
            return;
        }

        logger.LogInformation("[{Url}] ChatGPT running simple prompt...", url);
        var isDotNetRole = await ExecuteSimplePrompt(url, jobDescription);
        if (!isDotNetRole)
        {
            progress.RecordSuccess();
            logger.LogInformation("[{Url}] ChatGPT - Noe matching role", url);
            return;
        }

        logger.LogInformation("[{Url}] ChatGPT running full prompt...", url);
        await ExecuteFullPrompt(url, jobDescription);
        progress.RecordSuccess();
    }

    private async Task<string?> GetJobDescription(string url)
    {
        await using var chromium = chromiumFactory.Create();
        var page = await chromium.FetchAsync(url);
        await page.WaitForTextAsync("Job Description");
        return await page.InnerTextAsync("body");
    }

    private async Task<bool> ExecuteSimplePrompt(string url, string jobDescription)
    {
        // Evaluate if It looks like a good match.
        var input = prompt.GetPrompt("simple-prompt", jobDescription);
        var aiResponse = await openAiApi.AskAsync(input, "gpt-5-nano");

        if (aiResponse.Contains("false"))
        {
            jobStorage.Store(new Job
            {
                Url = url,
                JobDescription = jobDescription,
                Message = "Not a good match",
                Score = 0,
            });

            return false;
        }

        return true;
    }

    private async Task ExecuteFullPrompt(string url, string jobDescription)
    {
        // Analyze with AI
        logger.LogInformation("[{Url}] Requesting analysis from ChatGPT...", url);
        var input = prompt.GetPrompt("full-prompt", jobDescription);
        var aiResponse = await openAiApi.AskAsync(input, "gpt-5-mini");

        var values = StringKeyValueParser.Parse(aiResponse);

        var job = new Job
        {
            Url = url,
            JobDescription = jobDescription,
            Message = aiResponse,
            JobTitle = values.Get("jobTitle"),
            CompanyName = values.Get("companyName"),
            Location = values.Get("location"),
            Remote = values.Get("remote"),
            IsDotNetRole = int.Parse(values.Get("isDotNetRole") ?? "-1"),
            MacroserviceScore = values.Get("microservicesScore"),
            ContractType = values.Get("contractType"),
            Seniority = values.Get("seniority"),
            Currency = values.Get("currency"),
            HourlyMin = values.Get("hourlyMin"),
            HourlyMax = values.Get("hourlyMax"),
            SalaryIsEstimated = values.Get("salaryIsEstimated"),
            SalaryOriginalText = values.Get("salaryOriginalText"),
            DeliveryPressureScore = values.Get("deliveryPressureScore"),
            TechKeywords = values.Get("techKeywords"),
            Confidence = values.Get("confidence"),
            Notes = values.Get("notes"),
            Score = int.Parse(values.Get("score") ?? "-1"),
        };

        jobStorage.Store(job);
    }
}