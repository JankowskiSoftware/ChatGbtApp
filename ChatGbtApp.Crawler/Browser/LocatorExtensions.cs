using Microsoft.Playwright;

namespace ChatGgtApp.Crawler.Browser;

public static class LocatorExtensions
{
    public static async IAsyncEnumerable<ILocator> EnumerateAsync(this ILocator locator)
    {
        var count = await locator.CountAsync();
        for (int i = 0; i < count; i++)
            yield return locator.Nth(i);
    }
}