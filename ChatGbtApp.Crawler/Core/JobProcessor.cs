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

    public async Task<LogCollector> ProcessJobAsync(JobUrl jobUrl)
    {
        var logger = new LogCollector();
        logger.LogInformation($"Starting job processing: [{jobUrl.JobTitle}], Url: [{jobUrl.Url}]");
        
        
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jobs = dbContext.Jobs.ToList();

        var url = jobUrl.Url;

        if (jobStorage.IsDuplicate(url))
        {
            progress.RecordDuplicate();
            logger.LogInformation($"Skipping duplicate job." );
            return logger;
        }

        var (jobPostingUrl,jobDescription) = await GetJobDescription(url);
        
        if (string.IsNullOrWhiteSpace(jobDescription))
        {
            progress.RecordEmpty();
            logger.LogWarning($"Empty content detected  skipping.");
            return logger;
        }
        
        if (!string.IsNullOrWhiteSpace(jobPostingUrl) &&  jobStorage.IsDuplicate(jobPostingUrl))
        {
            progress.RecordDuplicate();
            logger.LogInformation($"Url2 duplicate detected skipping.");
            return logger;
        }


        bool isDistributedBackend = IsDistributedBackend(jobUrl, jobDescription);
        if (!isDistributedBackend)
        {
            StoreNotMatchingRole(url, jobPostingUrl, jobDescription);
            progress.RecordReject();
            logger.LogInformation($"Rejected.");
            return logger;
        }
        
        logger.LogDebug($"ChatGPT running prompt");
        var job = await ExecuteFullPrompt(jobUrl, jobPostingUrl, jobDescription, logger);
        progress.RecordSuccess();
        logger.LogInformation($"Finished Job: [{jobUrl.JobTitle}], Company: [{job.CompanyName}]");
        logger.LogInformation($"   - Notes: {job.Notes}");
        logger.LogInformation($"   - Location: {job.Location}");
        logger.LogInformation($"   - Remote: {job.Remote}");
        logger.LogInformation($"   - IsDistributed: {job.IsDistributed}");
        logger.LogInformation($"   - Score: {job.Score}");
        
        return logger;
    }

    private async Task<(string? jobPostingUrl, string? jobDescription)> GetJobDescription(string url)
    {
        await using var chromium = chromiumFactory.Create();
        var page = await chromium.FetchAsync(url);

        // var htmlA = await page.SelectNodes("//div[contains(text(), 'Job Posting')]/parent::div/a");
        var htmlA = await page.SelectNodes("//div[@class = 'sm:w-8/12 list-none']//h3//a");
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
            Rejected = true
        });
    }

    private async Task<Job> ExecuteFullPrompt(JobUrl jobUrl, string? jobPostingUrl, string jobDescription, LogCollector logger)
    {
        var input = prompt.GetPrompt("prompt", jobDescription);
        var aiResponse = await openAiApi.AskAsync(input, "gpt-5-mini");

        var values = StringKeyValueParser.Parse(aiResponse);
        
        int.TryParse(values.Get("plRemote"), out var plRemote);
        
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
            PlRemote = plRemote,
            IsDistributed = int.Parse(values.Get("isDistributedBackand") ?? "-1"),
            ContractType = values.Get("contractType"),
            Seniority = values.Get("seniority"),
            Currency = values.Get("currency"),
            HourlyMin = values.Get("hourlyMin"),
            HourlyMax = values.Get("hourlyMax"),
            HourlyMaxPLN = ConvertToPln(values.Get("currency"), values.Get("hourlyMax")),
            SalaryOriginalText = values.Get("salaryOriginalText"),
            DeliveryPressureScore = values.Get("deliveryPressureScore"),
            TechKeywords = values.Get("techKeywords"),
            Confidence = values.Get("confidence"),
            Notes = values.Get("notes"),
            Score = int.Parse(values.Get("score") ?? "-1"),
        };

        jobStorage.Store(job);
        
        return job;
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
    
    private decimal ConvertToPln(string currency, string amount)
    {
        Decimal.TryParse(amount, out decimal amount1);
        
        decimal rate = currency switch
        {
            "GBP" => 4.8m, // British Pound → PLN
            "USD" => 3.6m, // US Dollar → PLN
            "EUR" => 4.2m, // Euro → PLN
            _ => 0
        };

        return amount1 * rate;
    }
}