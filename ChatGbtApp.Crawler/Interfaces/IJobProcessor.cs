using ChatGgtApp.Crawler.Parsers;

namespace ChatGgtApp.Crawler.Interfaces;

/// <summary>
/// Processes job postings, including content extraction, AI analysis, and storage.
/// </summary>
public interface IJobProcessor
{
    /// <summary>
    /// Processes a single job posting from the given URL.
    /// </summary>
    Task<JobProcessingResult> ProcessJobAsync(string url);
}

/// <summary>
/// Result of processing a job posting.
/// </summary>
public class JobProcessingResult
{
    public bool Success { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public ParsedJobFit? ParsedData { get; set; }
    public bool WasDuplicate { get; set; }
    public bool WasEmpty { get; set; }
}
