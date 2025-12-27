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

            var html = await page.ContentAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // var rows = doc.DocumentNode.SelectNodes("//tbody/tr");
            // foreach (var row in rows)
            // {
            //     var link = doc.DocumentNode.SelectNodes("//td[2]/a").First();
            //     var company = doc.DocumentNode.SelectNodes("//td[3]").First().InnerHtml;
            //     var location = doc.DocumentNode.SelectNodes("//td[4]").First().InnerHtml;
            //     var platform = doc.DocumentNode.SelectNodes("//td[6]").First().InnerHtml;
            //
            //     string jobTitle = link.InnerText.Trim();
            //     string url = link.GetAttributeValue("href", "");
            //     
            //     results.Add(new JobLink(
            //         jobTitle,
            //         LoopcvConst.MainUrl + url,
            //         company,
            //         location,
            //         platform)
            //     );
            // }


            var jobLinks = doc.DocumentNode.SelectNodes("//tbody/tr/td[2]/a");
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
    // public record JobLink(string JobTitle, string Url, string CompanyName, string Location, string Platform);

    // public async Task<List<JobLink>> GetMatchUrlsAsync(IPage page, ILocator nextButton)
    // {
    //     var nextButton = page.GetByRole(
    //         AriaRole.Button,
    //         new() { Name = "Next page" }
    //     );
    //
    //     await nextButton.WaitForAsync();
    //
    //     var ariaDisabled = await nextButton.GetAttributeAsync("aria-disabled");
    //     if (ariaDisabled == "true")
    //     {
    //         return results;
    //     }
    //
    //
    
    //     // recursive next page
    //     /////////////////
    //
    //

    //    div[contains(@class, 'class-name-1')]

    //     var task = page.Locator($"div.v-data-table--loading")
    //         .WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
    //
    //     await nextButton.ClickAsync();
    //
    //     await task;
    //
    //
    //     await page.WaitForLoadingCycleAsync("div.v-data-table", "div.v-data-table:not(.v-data-table--loading)");
    //
    //     // var table = page.Locator("div.v-data-table");
    //     // await table.WaitForLoadingCycleAsync("v-data-table--loading");
    //     //
    //     // div.v-data-table
    //     // div.v-data-table:not(.v-table)
    //
    //     var html = await page.ContentAsync();
    //     var doc = new HtmlDocument();
    //     doc.LoadHtml(html);
    //
    //     var jobLinks = doc.DocumentNode.SelectNodes("//tbody/tr/td[2]/a");
    //     if (jobLinks == null)
    //     {
    //         throw new Exception("No job links found on matches page");
    //     }
    //
    //     var results = new List<JobLink>();
    //     foreach (var link in jobLinks)
    //     {
    //         string text = link.InnerText.Trim();
    //         string url = link.GetAttributeValue("href", "");
    //         results.Add(new JobLink(text, LoopcvConst.MainUrl + url));
    //     }
    //
    //     var nextPageButton = doc.DocumentNode.SelectSingleNode("//button[@aria-label='Next page']");
    //     if (nextPageButton != null)
    //     {
    //     }
    //
    //     return results;
    // }
}