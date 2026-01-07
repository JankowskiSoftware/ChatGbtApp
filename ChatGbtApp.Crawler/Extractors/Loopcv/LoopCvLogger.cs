using ChatGgtApp.Crawler.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace ChatGgtApp.Crawler.Extractors.Loopcv;

public class LoopCvLogger(
    SecretsLoader secretsLoader,
    IConfiguration configuration,
    ILogger<LoopCvLogger> logger)
{
    private const string AuthStatePath = "auth.json";
    
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
        var loginForm = page.Locator("form[data-id='sign-in-form']");
        if (await loginForm.IsVisibleAsync(new() { Timeout = 2000 }))
        {
            logger.LogWarning("Login form detected on page, auth likely expired.");
            return false;
        }

        return true;
    }

    public async Task LogIn(IPage page)
    {
        
        //await page.GotoAsync(loginUrl);

        var (username, password) = secretsLoader.Load(configuration["CredentialName"]);
         
        // Wait for email input by placeholder (handles JS/redirects)
        // var emailInput = page.Locator("form[data-id='sign-in-form'] input:nth-of-type(2)");



        var e = page.Locator("form[data-id='sign-in-form'] input:not([type='hidden'])");
        
        var emailInput = page
            .Locator("form[data-id='sign-in-form'] input:not([type='hidden'])")
            .Nth(0);
        await emailInput.WaitForAsync(); // waits until visible [web:95][web:101]
        
        await emailInput.FillAsync(username);

        await  page
            .Locator("form[data-id='sign-in-form'] input:not([type='hidden'])")
            .Nth(1)
            .FillAsync(password);

        // Click the "Sign in" button
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        // Optional: wait for post-login navigation or some element that indicates success
        await page.WaitForURLAsync("**/jobs");
        // or: await page.WaitForSelectorAsync("css=selector-for-logged-in-ui");

        logger.LogDebug("Saved auth state to {Path}", AuthStatePath);
    }
}