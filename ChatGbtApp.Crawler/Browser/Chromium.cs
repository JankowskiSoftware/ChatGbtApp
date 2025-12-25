
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace ChatGgtApp.Crawler.Browser;

// DTO so caller knows if we got logged out instead of real content
public class FetchResult
{
    public bool IsLoggedOut { get; set; }
    public string? Content { get; set; }
}

public class Chromium
{
    private readonly string _loginUrl;
    private readonly ILogger<Chromium> _logger;

    private readonly string _authStatePath;
    private readonly string _userAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
    private readonly ViewportSize _viewport = new() { Width = 1920, Height = 1080 };

    public Chromium(string loginUrl, ILogger<Chromium> logger, string authStatePath = "auth.json")
    {
        _loginUrl = loginUrl;
        _logger = logger;
        _authStatePath = authStatePath;
    }

    public async Task BootstrapLoginAsync(string email, string password)
    {
        using var pw = await Playwright.CreateAsync();

        var browser = await pw.Chromium.LaunchAsync(new()
        {
            Headless = false
        });

        var context = await browser.NewContextAsync(new()
        {
            UserAgent = _userAgent,
            ViewportSize = _viewport
        });

        var page = await context.NewPageAsync();

        await page.GotoAsync(_loginUrl);

        // Wait for email input by placeholder (handles JS/redirects)
        var emailInput = page.GetByPlaceholder("Type your Email");
        await emailInput.WaitForAsync(); // waits until visible [web:95][web:101]

        // Fill email and password
        await emailInput.FillAsync(email);
        await page.GetByPlaceholder("Type your Password").FillAsync(password);

        // Click the "Sign in" button
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        // Optional: wait for post-login navigation or some element that indicates success
        await page.WaitForURLAsync("**/overview");
        // or: await page.WaitForSelectorAsync("css=selector-for-logged-in-ui");

        await context.StorageStateAsync(new()
        {
            Path = _authStatePath
        });

        _logger.LogInformation("Saved auth state to {Path}", _authStatePath);

        await browser.CloseAsync();
    }


    /// <summary>
    /// Fetches a page using stored auth state (auth.json if present).
    /// Returns IsLoggedOut = true if we see a login page instead of content.
    /// </summary>
    public async Task<FetchResult> FetchAsync(string targetUrl)
    {
        using var pw = await Playwright.CreateAsync();
        await using var browser = await pw.Chromium.LaunchAsync();

        var context = await browser.NewContextAsync(new()
        {
            StorageStatePath = File.Exists(_authStatePath) ? _authStatePath : null,
            UserAgent = _userAgent,
            ViewportSize = _viewport
        });

        var page = await context.NewPageAsync();

        await page.GotoAsync(targetUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });

        // --- Detect login instead of real page ---

        // 1) URL-based check
        var url = page.Url;
        if (url.Contains("/login", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("signin", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Navigated to login page instead of target: {Url}", url);
            await context.CloseAsync();
            return new FetchResult { IsLoggedOut = true };
        }

        // 2) DOM-based check (adjust selectors to your site)
        var loginForm = page.Locator("form[action*='login']");
        try
        {
            if (await loginForm.IsVisibleAsync(new() { Timeout = 2000 }))
            {
                _logger.LogWarning("Login form detected on page, auth likely expired.");
                await context.CloseAsync();
                return new FetchResult { IsLoggedOut = true };
            }
        }
        catch (TimeoutException)
        {
            // Ignore if not found in time
        }

        // If we got here, assume we are authenticated and on the target page
        var content = await page.InnerTextAsync("body");

        await context.CloseAsync();

        return new FetchResult
        {
            IsLoggedOut = false,
            Content = content
        };
    }
}
