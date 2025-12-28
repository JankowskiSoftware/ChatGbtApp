using HtmlAgilityPack;
using Microsoft.Playwright;

namespace ChatGgtApp.Crawler.Browser;

public static  class PageExtensions
{
    public static async Task<HtmlNodeCollection> SelectNodes(this IPage page, string xpath)
    {
        await page.WaitForSelectorAsync(xpath,
            new PageWaitForSelectorOptions
            {
                Timeout = 15_000,
                State = WaitForSelectorState.Visible
            }
        );
        
        var html = await page.ContentAsync();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        return doc.DocumentNode.SelectNodes(xpath);
    }   
}