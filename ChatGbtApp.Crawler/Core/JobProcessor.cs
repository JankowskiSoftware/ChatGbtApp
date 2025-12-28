using ChatGbtApp;
using ChatGbtApp.Interfaces;
using ChatGbtApp.Repository;
using ChatGgtApp.Crawler.Browser;
using ChatGgtApp.Crawler.Parsers;
using ChatGgtApp.Crawler.Progress;
using ChatGgtApp.Crawler.Storage;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

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
    JobProcessingProgress progress,
    IServiceScopeFactory scopeFactory)
{
    private string[] _kewords = new[]
    {
        ".NET",
        "C#",
        "dot",
        "Sharp"
    };

    public async Task<List<string>> ProcessJobAsync(JobUrl jobUrl)
    {
        var logs = new List<string>();
        
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jobs = dbContext.Jobs.ToList();

        var url = jobUrl.Url;

        if (jobStorage.IsDuplicate(url))
        {
            progress.RecordDuplicate();
            logger.LogInformation("[{Url}] Skipping duplicate job", url);
            return;
        }

        var (jobPostingUrl,jobDescription) = await GetJobDescription(url);
        
        if (string.IsNullOrWhiteSpace(jobDescription))
        {
            progress.RecordEmpty();
            logger.LogWarning("[{Url}] Empty content detected; skipping store.", url);
            return;
        }
        
        if (!string.IsNullOrWhiteSpace(jobPostingUrl) &&  jobStorage.IsDuplicate(jobPostingUrl))
        {
            progress.RecordDuplicate();
            logger.LogInformation("[{Url}] Skipping duplicate posting job", url);
            return;
        }

        logger.LogDebug("[{Url}] Starting job processing...", url);

        bool isDistributedBackend = IsDistributedBackend(jobUrl, jobDescription);
        if (!isDistributedBackend)
        {
            StoreNotMatchingRole(url, jobPostingUrl, jobDescription);
            progress.RecordSuccess();
            logger.LogInformation("[{Url}] ChatGPT - Noe matching role", url);
            return;
        }
        
        logger.LogDebug("[{Url}] ChatGPT running full prompt...", url);
        await ExecuteFullPrompt(jobUrl, jobPostingUrl, jobDescription);
        progress.RecordSuccess();
    }

    private async Task<(string? jobPostingUrl, string? jobDescription)> GetJobDescription(string url)
    {
        await using var chromium = chromiumFactory.Create();
        var page = await chromium.FetchAsync(url);

        var htmlA = await page.SelectNodes("//div[contains(text(), 'Job Posting')]/parent::div/a");
        string jobPostingUrl = htmlA.First().GetAttributeValue("href", "");
        
        await page.WaitForTextAsync("Job Description");
        var jobDescription = await page.InnerTextAsync("body");
        
        return (jobPostingUrl, jobDescription);
    }

    private void StoreNotMatchingRole(string url, string jobPostingUrl, string jobDescription)
    {
        jobStorage.Store(new Job
        {
            Url = url,
            Url2 = jobPostingUrl,
            JobDescription = jobDescription,
            IsDistributed = 0,
            Score = 0,
        });
    }

    private async Task ExecuteFullPrompt(JobUrl jobUrl, string? jobPostingUrl, string jobDescription)
    {
        // Analyze with AI
        logger.LogDebug("[{Url}] Requesting analysis from ChatGPT...", jobUrl.Url);
        var input = prompt.GetPrompt("prompt", jobDescription);
        var aiResponse = await openAiApi.AskAsync(input, "gpt-5-mini");

        var values = StringKeyValueParser.Parse(aiResponse);

        var job = new Job
        {
            Url = jobUrl.Url,
            Url2 = jobPostingUrl,
            JobDescription = jobDescription,
            Message = aiResponse,
            JobTitle = jobUrl.JobTitle,
            CompanyName = values.Get("companyName"),
            Location = values.Get("location"),
            Remote = values.Get("remote"),
            IsDistributed = int.Parse(values.Get("isDistributedBackand") ?? "-1"),
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

    private bool IsDistributedBackend(JobUrl jobUrl, string jobDescription)
    {
        bool isDotNetRole = false;
        foreach (var keword in _kewords)
        {
            if (jobUrl.JobTitle.Contains(keword, StringComparison.OrdinalIgnoreCase)
                || jobDescription.Contains(keword, StringComparison.OrdinalIgnoreCase))
            {
                isDotNetRole = true;
                break;
            }
        }

        return isDotNetRole;
    }
}