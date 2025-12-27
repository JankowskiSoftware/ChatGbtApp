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
var links = jobs
    .Select(jobUrl => jobUrl.Split(',').Last().Trim())
    .Take(10)
    .ToList();

await crawler.CrawlJobsAsync(links);