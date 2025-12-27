using ChatGgtApp.Crawler.Extractors.Loopcv;
using Microsoft.Playwright;

namespace ChatGgtApp.Crawler.Browser;

public class Chromium(
    LoopCvLogger loopCvLogger,
    string authStatePath = "auth.json") : IAsyncDisposable
{
    private readonly string _userAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    private readonly ViewportSize _viewport = new() { Width = 1920, Height = 1080 };

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;


    public async Task<IPage> FetchAsync(string targetUrl, bool hideBrowser = true)
    {
        if (_context == null)
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = hideBrowser,
                //     SlowMo = 50                    
            });
            _context = await _browser.NewContextAsync(new()
            {
                StorageStatePath = File.Exists(authStatePath) ? authStatePath : null,
                UserAgent = _userAgent,
                ViewportSize = _viewport
            });
        }

        var page = await _context.NewPageAsync();
        await page.GotoAsync(targetUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });

        if (!await loopCvLogger.IsLoggedInAsync(page))
        {
            await loopCvLogger.LogIn(page);
            await _context.StorageStateAsync(new()
            {
                Path = authStatePath
            });

            try
            {
                await _context.CloseAsync();
            }
            catch
            {
                
            }

            _context = await _browser!.NewContextAsync(new()
            {
                StorageStatePath = File.Exists(authStatePath) ? authStatePath : null,
                UserAgent = _userAgent,
                ViewportSize = _viewport
            });

            page = await _context.NewPageAsync();
            await page.GotoAsync(targetUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            if (!await loopCvLogger.IsLoggedInAsync(page))
                throw new Exception("Failed to log in");
        }

        return page;

        // await page.WaitForTextCycleAsync("Loading items...");
        // var textContent = await page.InnerTextAsync("body");
        // var html = await page.ContentAsync();
        // try { await page.CloseAsync(); } catch { }
        //
        // return new FetchResult { TextContent = textContent, Html = html };
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_context != null) await _context.CloseAsync();
        }
        catch
        {
        }

        try
        {
            if (_browser != null) await _browser.CloseAsync();
        }
        catch
        {
        }

        try
        {
            _playwright?.Dispose();
        }
        catch
        {
        }
    }
}