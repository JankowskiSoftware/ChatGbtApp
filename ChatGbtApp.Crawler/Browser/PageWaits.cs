namespace ChatGgtApp.Crawler.Browser;

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;

// USAGE EXAMPLES
//
// await page.WaitPageUsableAsync();                           // generic baseline
// await page.WaitForTextAsync("Welcome");                     // exact text
// await page.WaitForTextAsync(new Regex("next page", RegexOptions.IgnoreCase));
// await page.WaitForCssOrXPathAsync("//*[@aria-label='Next page']");
// await page.WaitForSelectorAsync("[aria-label='Next page']"); // CSS
// await page.WaitForBodyTextAsync(new Regex("Results:\\s+\\d+"));
//
// var ok = await PageWaits.TryWaitAsync(() => page.WaitForTextAsync("Loaded"), timeoutMs: 5000);

public static class LoadingWaits
{
    // Wait until a given text is NOT visible anymore (hidden or removed).
    public static Task WaitForTextToDisappearAsync(this IPage page, string text, int timeoutMs = 15_000) =>
        page.GetByText(text).First.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = timeoutMs });

    // Regex variant (handy when loading text changes slightly).
    public static Task WaitForTextToDisappearAsync(this IPage page, Regex textRegex, int timeoutMs = 15_000) =>
        page.GetByText(textRegex).First
            .WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = timeoutMs });

    // Wait for some "real" element to become visible (recommended when possible).
    public static Task WaitForVisibleAsync(this IPage page, string cssSelector, int timeoutMs = 15_000) =>
        page.Locator(cssSelector).First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeoutMs });

    // If you only have XPath.
    public static Task WaitForVisibleXPathAsync(this IPage page, string xpath, int timeoutMs = 15_000) =>
        page.Locator("xpath=" + xpath).First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeoutMs });

    // Wait until a selector is detached from DOM entirely (strict removal).
    public static Task WaitForDetachedAsync(this IPage page, string cssSelector, int timeoutMs = 15_000) =>
        page.Locator(cssSelector).First
            .WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = timeoutMs });
}

public static class PageWaits
{
    public static async Task WaitForTextCycleAsync(
        this IPage page,
        string text,
        int timeoutAppearMs = 15_000,
        int timeoutDisappearMs = 15_000)
    {
        await page.WaitUntilTextAppearsAsync(text, timeoutAppearMs);
        await page.WaitUntilTextDisappearsAsync(text, timeoutDisappearMs);
    }

    public static Task WaitUntilTextAppearsAsync(
        this IPage page,
        string text,
        int timeoutMs = 15_000)
    {
        return page.WaitForFunctionAsync(
            @"(t) => {
            const body = document.body;
            return !!body && (body.innerText || '').includes(t);
        }",
            text,
            new() { Timeout = timeoutMs }
        );
    }

    public static Task WaitUntilTextDisappearsAsync(
        this IPage page,
        string text,
        int timeoutMs = 15_000)
    {
        return page.WaitForFunctionAsync(
            @"(t) => {
            const body = document.body;
            return !body || !(body.innerText || '').includes(t);
        }",
            text,
            new() { Timeout = timeoutMs }
        );
    }
    
    
    
    
    
    
    
    public static async Task WaitForLoadingCycleAsync(
        this IPage page,
        string cssSelector,
        string loadingClass = "loading",
        int timeoutAppearMs = 15_000,
        int timeoutDisappearMs = 60_000)
    {
        await page.WaitUntilHasClassAsync(cssSelector, loadingClass, timeoutAppearMs);
        await page.WaitUntilMissingClassAsync(cssSelector, loadingClass, timeoutDisappearMs);
    }

    
    
    
    public static Task WaitUntilHasClassAsync(
        this IPage page,
        string cssSelector,
        string className,
        int timeoutMs = 15_000)
    {
        return page.Locator($"{cssSelector}.{className}")
            .WaitForAsync(new LocatorWaitForOptions { Timeout = timeoutMs });
    }

    public static Task WaitUntilMissingClassAsync(
        this IPage page,
        string cssSelector,
        string className,
        int timeoutMs = 15_000)
    {
        return page.Locator($"{cssSelector}:not(.{className})")
            .WaitForAsync(new LocatorWaitForOptions { Timeout = timeoutMs });
    }

    
    
    

    

    // 1) Load-state waits

    public static Task WaitDomReadyAsync(this IPage page, int timeoutMs = 15_000) =>
        page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new() { Timeout = timeoutMs });

    public static async Task WaitNetworkIdleAsync(this IPage page, int timeoutMs = 15_000)
    {
        // Many SPAs never truly go idle; keep it optional where you call it.
        try
        {
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = timeoutMs });
        }
        catch (TimeoutException)
        {
            // Intentionally ignored
        }
    }

    // 2) Element-exists waits (CSS or XPath)

    public static Task WaitForSelectorAsync(this IPage page, string cssSelector, int timeoutMs = 15_000) =>
        page.Locator(cssSelector).First.WaitForAsync(new() { Timeout = timeoutMs });

    public static Task WaitForXPathAsync(this IPage page, string xpath, int timeoutMs = 15_000) =>
        page.Locator("xpath=" + xpath).First.WaitForAsync(new() { Timeout = timeoutMs });

    public static Task WaitForCssOrXPathAsync(this IPage page, string selectorOrXPath, int timeoutMs = 15_000)
    {
        var s = selectorOrXPath.Trim();
        var locator = LooksLikeXPath(s)
            ? page.Locator(s.StartsWith("xpath=", StringComparison.OrdinalIgnoreCase) ? s : "xpath=" + s)
            : page.Locator(s);

        return locator.First.WaitForAsync(new() { Timeout = timeoutMs });
    }

    // 3) Text waits

    public static Task WaitForTextAsync(this IPage page, string text, int timeoutMs = 15_000) =>
        page.GetByText(text).First.WaitForAsync(new() { Timeout = timeoutMs });

    public static Task WaitForTextAsync(this IPage page, Regex textRegex, int timeoutMs = 15_000) =>
        page.GetByText(textRegex).First.WaitForAsync(new() { Timeout = timeoutMs });

    /// <summary>
    /// Wait until body.innerText matches a regex (useful when text is spread across multiple nodes).
    /// </summary>
    public static Task WaitForBodyTextAsync(this IPage page, Regex regex, int timeoutMs = 15_000)
    {
        // Note: Passing a .NET Regex directly isn't supported, so we pass pattern+flags.
        var flags = regex.Options.HasFlag(RegexOptions.IgnoreCase) ? "i" : "";

        return page.WaitForFunctionAsync(
            @"(arg) => {
                const re = new RegExp(arg.pattern, arg.flags);
                const t = document.body ? (document.body.innerText || '') : '';
                return re.test(t);
              }",
            new { pattern = regex.ToString(), flags },
            new() { Timeout = timeoutMs }
        );
    }

    // 4) Composition helpers (optional)

    public static async Task WaitPageUsableAsync(this IPage page, int timeoutMs = 15_000)
    {
        await page.WaitDomReadyAsync(timeoutMs);
        await page.WaitForSelectorAsync("body", timeoutMs);
    }

    // 5) Utility

    public static async Task<bool> TryWaitAsync(Func<Task> wait, int timeoutMs)
    {
        try
        {
            var finished = await Task.WhenAny(wait(), Task.Delay(timeoutMs));
            return finished is Task t && t.Status == TaskStatus.RanToCompletion;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    private static bool LooksLikeXPath(string s) =>
        s.StartsWith("//") || s.StartsWith("(//") || s.StartsWith("./") || s.StartsWith("..") || s.Contains("//*[");
}