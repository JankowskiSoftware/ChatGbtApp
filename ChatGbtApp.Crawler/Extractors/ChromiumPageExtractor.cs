using ChatGgtApp.Crawler.Browser;
using ChatGgtApp.Crawler.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChatGgtApp.Crawler.Extractors;

/// <summary>
/// Extracts job content from authenticated job sites using Chromium automation.
/// Handles login requirements and authentication persistence.
/// </summary>
public class ChromiumPageExtractor : IPageContentExtractor
{
    private readonly Chromium _chromium;
    private readonly ILogger<ChromiumPageExtractor> _logger;
    private readonly string _emailEnvVar;
    private readonly string _passwordEnvVar;
    private readonly Func<string, bool> _urlMatcher;

    public ChromiumPageExtractor(
        Chromium chromium,
        ILogger<ChromiumPageExtractor> logger,
        string emailEnvVar = "LOOP_EMAIL",
        string passwordEnvVar = "LOOP_PASS",
        Func<string, bool>? urlMatcher = null)
    {
        _chromium = chromium;
        _logger = logger;
        _emailEnvVar = emailEnvVar;
        _passwordEnvVar = passwordEnvVar;
        _urlMatcher = urlMatcher ?? (_ => true); // Default: handles all URLs
    }

    public bool CanHandle(string url) => _urlMatcher(url);

    public async Task<PageExtractionResult> ExtractAsync(string url)
    {
        try
        {
            _logger.LogInformation("Extracting content from: {Url}", url);
            
            var result = await _chromium.FetchAsync(url);

            if (result.IsLoggedOut)
            {
                _logger.LogWarning("Authentication required for {Url}. Attempting login...", url);
                
                var email = Environment.GetEnvironmentVariable(_emailEnvVar);
                var password = Environment.GetEnvironmentVariable(_passwordEnvVar);

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    return PageExtractionResult.Failed(
                        $"Missing credentials: {_emailEnvVar} or {_passwordEnvVar} not set");
                }

                await _chromium.BootstrapLoginAsync(email, password);
                
                // Retry after login
                result = await _chromium.FetchAsync(url);

                if (result.IsLoggedOut)
                {
                    return PageExtractionResult.Failed("Authentication failed after login attempt");
                }
            }

            if (string.IsNullOrWhiteSpace(result.Content))
            {
                return PageExtractionResult.Failed("Extracted content is empty");
            }

            return PageExtractionResult.Succeeded(result.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting content from {Url}", url);
            return PageExtractionResult.Failed(ex.Message);
        }
    }
}
