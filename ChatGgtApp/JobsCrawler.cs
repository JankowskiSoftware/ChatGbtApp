namespace ChatGgtApp;

using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;

public class JobsCrawler
{
    public async Task CrawlJobs(string urls)
    {
        await SaveLoginStateAsync("https://app.loopcv.pro/login");
        
        var links = urls.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var url in links)
        {
            // Use the renderer that waits for JS if possible; falls back to static scraping
            var page = await PrintRenderedPageTextAsync(url);
            Console.WriteLine($"--- Content from: {url} ---");
            Console.WriteLine(page);
            Console.WriteLine();
        }
    }
    
    public static async Task SaveLoginStateAsync(string loginUrl, string statePath = "state.json")
    {
        using var pw = await Playwright.CreateAsync();
        await using var browser = await pw.Chromium.LaunchAsync(new() { Headless = false });

        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(loginUrl);

        Console.WriteLine("Log in manually, then press ENTER here...");
        Console.ReadLine();

        await context.StorageStateAsync(new() { Path = statePath });
    }
    
    public static async Task<string> FetchWithSavedStateAsync(string url, string statePath = "state.json")
    {
        using var pw = await Playwright.CreateAsync();
        await using var browser = await pw.Chromium.LaunchAsync(new() { Headless = true });

        var context = await browser.NewContextAsync(new()
        {
            StorageStatePath = statePath
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });

        return await page.InnerTextAsync("body");
    }



    public async Task<string> PrintRenderedPageTextAsync(string url, int waitAfterLoadSeconds = 2, string statePath = "state.json")
    {
        try
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var context = await browser.NewContextAsync(new()
            {
                StorageStatePath = statePath
            });
            var page = await context.NewPageAsync();

            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });

            if (waitAfterLoadSeconds > 0)
            {
                await page.WaitForTimeoutAsync(waitAfterLoadSeconds * 1000);
            }

            var bodyText = await page.InnerTextAsync("body");
            var cleaned = WebUtility.HtmlDecode(Regex.Replace(bodyText ?? string.Empty, @"\s+", " ").Trim());
            return cleaned;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Playwright fetch failed for '{url}': {ex.Message}. Falling back to HTTP scraping.");
            return await PrintPageTextAsync(url);
        }
    }

    public async Task<string> PrintPageTextAsync(string url)
    {
        using var client = new HttpClient();
        string html;
        try
        {
            html = await client.GetStringAsync(url);
            
        }
        catch (Exception ex)
        {
            throw new Exception($"Error fetching '{url}': {ex.Message}", ex);
        }

        // Remove <script>...</script>, <style>...</style>, and HTML comments
        html = Regex.Replace(html, @"(?is)<script.*?>.*?</script>", string.Empty);
        html = Regex.Replace(html, @"(?is)<style.*?>.*?</style>", string.Empty);
        html = Regex.Replace(html, @"(?is)<!--.*?-->", string.Empty);

        // Remove all remaining tags
        var textOnly = Regex.Replace(html, @"<[^>]+>", " ");

        // Decode HTML entities and collapse whitespace
        textOnly = WebUtility.HtmlDecode(textOnly);
        textOnly = Regex.Replace(textOnly, @"\s+", " ").Trim();
        
        return textOnly;
    }
}