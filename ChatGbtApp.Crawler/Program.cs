// See https://aka.ms/new-console-template for more information


using ChatGbtApp;
using ChatGgtApp.Crawler.Core;
using Microsoft.Extensions.Logging;

ServiceContainer.Configure();

var loggerFactory = ServiceContainer.Resolve<ILoggerFactory>();
var logger = loggerFactory.CreateLogger("Crawler");
logger.LogInformation("Crawler started...");


// await  ServiceContainer
//     .Resolve<MatchesCrawler>()
//     .CrawlAsync("https://app.loopcv.pro/matches");


var crawler = ServiceContainer.Resolve<JobsCrawler>();
var jobsPath = SolutionDirectory.GetRepoPath(SolutionDirectory.Path_JobUrls);

var jobs = File.ReadAllLines(jobsPath);
var jobUrls = jobs
    .Select(jobUrl =>
    {
        var comma = jobUrl.LastIndexOf(',');
        var jobTitle = jobUrl.Substring(0, comma);
        var url = jobUrl.Substring(comma + 1).Trim();
        
        return new JobUrl(url, jobTitle);

    })
    //.Take(100)
    .ToList();

await crawler.CrawlJobsAsync(jobUrls);