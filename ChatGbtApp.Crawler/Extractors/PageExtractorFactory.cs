using ChatGgtApp.Crawler.Interfaces;

namespace ChatGgtApp.Crawler.Extractors;

/// <summary>
/// Factory for creating appropriate page content extractors based on URL patterns.
/// Allows registration of multiple extractors for different sites.
/// </summary>
public class PageExtractorFactory
{
    private readonly List<IPageContentExtractor> _extractors = new();
    private IPageContentExtractor? _defaultExtractor;

    /// <summary>
    /// Registers an extractor that will be checked in order of registration.
    /// </summary>
    public void RegisterExtractor(IPageContentExtractor extractor)
    {
        _extractors.Add(extractor);
    }

    /// <summary>
    /// Sets a default extractor to use when no specific extractor can handle the URL.
    /// </summary>
    public void SetDefaultExtractor(IPageContentExtractor extractor)
    {
        _defaultExtractor = extractor;
    }

    /// <summary>
    /// Gets the appropriate extractor for the given URL.
    /// Returns the first registered extractor that can handle the URL,
    /// or the default extractor if none match.
    /// </summary>
    public IPageContentExtractor GetExtractor(string url)
    {
        var extractor = _extractors.FirstOrDefault(e => e.CanHandle(url));
        
        if (extractor != null)
            return extractor;

        if (_defaultExtractor != null)
            return _defaultExtractor;

        throw new InvalidOperationException(
            $"No extractor found for URL: {url}. Please register an appropriate extractor.");
    }
}
