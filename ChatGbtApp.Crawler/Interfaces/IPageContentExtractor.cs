namespace ChatGgtApp.Crawler.Interfaces;

/// <summary>
/// Extracts content from web pages using browser automation.
/// Different implementations can handle different sites/structures.
/// </summary>
public interface IPageContentExtractor
{
    /// <summary>
    /// Extracts text content from the specified URL.
    /// </summary>
    /// <param name="url">The URL to extract content from.</param>
    /// <returns>The extracted content, or null if extraction failed.</returns>
    Task<PageExtractionResult> ExtractAsync(string url);
    
    /// <summary>
    /// Determines if this extractor can handle the given URL.
    /// </summary>
    bool CanHandle(string url);
}

/// <summary>
/// Result of a page content extraction operation.
/// </summary>
public class PageExtractionResult
{
    public bool Success { get; set; }
    public string? Content { get; set; }
    public bool RequiresAuthentication { get; set; }
    public string? ErrorMessage { get; set; }
    
    public static PageExtractionResult Succeeded(string content) => new()
    {
        Success = true,
        Content = content
    };
    
    public static PageExtractionResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
    
    public static PageExtractionResult AuthenticationRequired() => new()
    {
        Success = false,
        RequiresAuthentication = true,
        ErrorMessage = "Authentication required"
    };
}
