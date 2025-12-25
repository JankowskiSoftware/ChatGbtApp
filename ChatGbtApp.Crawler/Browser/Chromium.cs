using ChatGgtApp.Crawler.Extractors.Loopcv;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace ChatGgtApp.Crawler.Browser;

// DTO so the caller knows if we got logged out instead of real content
public class FetchResult
{
    public string? TextContent { get; set; }
    public string? Html { get; set; }
}

public class Chromium(
    ILogger<Chromium> logger,
    LoopCvLogger loopCvLogger,
    string authStatePath = "auth.json")
{
    private readonly string _userAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    private readonly ViewportSize _viewport = new() { Width = 1920, Height = 1080 };


    public async Task<FetchResult> FetchAsync(string targetUrl)
    {
        using var pw = await Playwright.CreateAsync();
        await using var browser = await pw.Chromium.LaunchAsync();
        var context = await browser.NewContextAsync(new()
        {
            StorageStatePath = File.Exists(authStatePath) ? authStatePath : null,
            UserAgent = _userAgent,
            ViewportSize = _viewport
        });

        // load target page
        var page = await context.NewPageAsync();
        await page.GotoAsync(targetUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });


        // first check if we're logged in
        if (!await loopCvLogger.IsLoggedInAsync(page))
        {
            await loopCvLogger.LogIn();
        }

        // reload page after login
        page = await context.NewPageAsync();
        await page.GotoAsync(targetUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });

        // second check if we're logged in
        if (!await loopCvLogger.IsLoggedInAsync(page))
        {
            // still not logged in, throw
            throw new Exception("Failed to log in");
        }

        // here we are logged in, save auth state and capture content before closing context
        var textContent = await page.InnerTextAsync("body");
        var html = await page.ContentAsync();

        await context.CloseAsync();

        return new FetchResult
        {
            TextContent = textContent,
            Html = html
        };
    }
}
