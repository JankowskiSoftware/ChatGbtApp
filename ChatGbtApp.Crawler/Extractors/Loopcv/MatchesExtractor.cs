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

            var jobLinks = await page.SelectNodes("//tbody/tr/td[2]/a"); 
            if (jobLinks == null)
            {
                throw new Exception("No job links found on matches page");
            }
            
            foreach (var link in jobLinks)
            {
                string jobTitle = link.InnerText.Trim();
                string url = link.GetAttributeValue("href", "");
                results.Add(new JobLink(jobTitle, LoopcvConst.MainUrl + url));
            }

            Console.WriteLine($"Loaded {results.Count} pages.");
        }


        return results;
    }

    public record JobLink(string JobTitle, string Url);
}