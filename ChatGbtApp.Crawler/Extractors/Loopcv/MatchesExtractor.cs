using ChatGbtApp;
using ChatGgtApp.Crawler.Browser;
using ChatGgtApp.Crawler.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace ChatGgtApp.Crawler.Extractors.Loopcv;

using HtmlAgilityPack;

public class MatchesExtractor(ChromiumFactory chromiumFactory)
{
    public async Task<List<JobLink>> GetMatchUrlsAsync(string matchesPageUrl)
    {
        var page = await chromiumFactory.Create()
            .FetchAsync(matchesPageUrl, false);


        var results = new List<JobLink>();

        while (true)
        {
            Console.WriteLine($"Do you want to continue? (y/n)");
            if (Console.ReadLine() == "n")
            {
                break;
            }
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await page.ScreenshotAsync(new() { Path = SolutionDirectory.GetRepoPath("page.png"), FullPage = true });
            File.WriteAllText( SolutionDirectory.GetRepoPath("page.html"),await page.ContentAsync());

            var jobLinks =  page.Locator("a[data-control-id]").EnumerateAsync(); 
            if (jobLinks == null)
            {
                throw new Exception("No job links found on matches page");
            }

            await foreach (var link in jobLinks)
            {
                string jobTitle = await link.InnerTextAsync();
                string? url = await link.GetAttributeAsync("href");
                results.Add(new JobLink(jobTitle, "https://www.linkedin.com/" + url));
            }

            Console.WriteLine($"Loaded {results.Count} pages.");
        }


        return results;
    }

    public record JobLink(string JobTitle, string Url);
}