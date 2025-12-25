using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace ChatGgtApp.Crawler.Extractors.Loopcv;

public class LoopCvLogger(
    ILogger<LoopCvLogger> logger,
    string loginUrl,
    string email,
    string password,
    string authStatePath = "auth.json")
{
    public async Task<bool> IsLoggedInAsync(IPage page)
    {
        // 1) URL-based check
        var url = page.Url;
        if (url.Contains("/login", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("signin", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Navigated to login page instead of target: {Url}", url);

            return false;
        }

        // 2) DOM-based check (adjust selectors to your site)
        var loginForm = page.Locator("form[action*='login']");
        if (await loginForm.IsVisibleAsync(new() { Timeout = 2000 }))
        {
            logger.LogWarning("Login form detected on page, auth likely expired.");
            return false;
        }

        return true;
    }

    public async Task LogIn()
    {
        using var pw = await Playwright.CreateAsync();

        var browser = await pw.Chromium.LaunchAsync(new()
        {
            Headless = false
        });

        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(loginUrl);

        // Wait for email input by placeholder (handles JS/redirects)
        var emailInput = page.GetByPlaceholder("Type your Email");
        await emailInput.WaitForAsync(); // waits until visible [web:95][web:101]

        // Fill email and password
        await emailInput.FillAsync(email);
        await page.GetByPlaceholder("Type your Password")
            .FillAsync(password);

        // Click the "Sign in" button
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        // Optional: wait for post-login navigation or some element that indicates success
        await page.WaitForURLAsync("**/overview");
        // or: await page.WaitForSelectorAsync("css=selector-for-logged-in-ui");

        await context.StorageStateAsync(new()
        {
            Path = authStatePath
        });

        logger.LogInformation("Saved auth state to {Path}", authStatePath);

        await browser.CloseAsync();
    }
}