using ChatGbtApp;
using ChatGbtApp.Interfaces;
using ChatGgtApp.Crawler.Interfaces;
using ChatGgtApp.Crawler.Parsers;
using ChatGgtApp.Crawler.Progress;
using ChatGgtApp.Crawler.Storage;
using Microsoft.Extensions.Logging;

namespace ChatGgtApp.Crawler.Core;

/// <summary>
/// Processes individual job postings by extracting content, analyzing with AI,
/// and storing results.
/// </summary>
public class JobProcessor(
    IPageContentExtractor pageContentExtractor,
    JobStorage jobStorage,
    IOpenAiApi openAiApi,
    GptKeyValueParser gptParser,
    Prompt prompt,
    ILogger<JobProcessor> logger,
    JobProcessingProgress progress)
    : IJobProcessor
{
    public async Task<JobProcessingResult> ProcessJobAsync(string url)
    {
        try
        {
            // Check for duplicates first
            if (jobStorage.IsDuplicate(url))
            {
                progress.RecordDuplicate();
                logger.LogInformation("[{Url}] Skipping duplicate job", url);
                return new JobProcessingResult
                { Success = true,
                    Url = url,
                    WasDuplicate = true
                };
            }

            logger.LogInformation("[{Url}] Starting job processing...", url);

            // Extract content
            var extractionResult = await pageContentExtractor.ExtractAsync(url);
            if (!extractionResult.Success)
            {
                return new JobProcessingResult
                {
                    Success = false,
                    Url = url,
                    ErrorMessage = extractionResult.ErrorMessage ?? "Content extraction failed",
                    WasEmpty = string.IsNullOrWhiteSpace(extractionResult.Content)
                };
            }

            var jobDescription = extractionResult.Content ?? string.Empty;
            if (string.IsNullOrWhiteSpace(jobDescription))
            {
                logger.LogWarning("[{Url}] Empty content detected; skipping store.", url);
                return new JobProcessingResult
                {
                    Success = false,
                    Url = url,
                    WasEmpty = true
                };
            }

            // Analyze with AI
            logger.LogInformation("[{Url}] Requesting analysis from ChatGPT...", url);
            var input = prompt.GetPrompt(jobDescription);
            var aiResponse = await openAiApi.AskAsync(input);

            // Parse AI response
            var parsedData = gptParser.ParseOrNull(aiResponse);
            if (parsedData == null)
            {
                logger.LogError("[{Url}] Failed to parse ChatGPT response; skipping.", url);
                return new JobProcessingResult
                {
                    Success = false,
                    Url = url,
                    ErrorMessage = "Failed to parse AI response"
                };
            }

            // Store results
            logger.LogInformation("[{Url}] Storing parsed results...", url);
            jobStorage.Store(url, jobDescription, aiResponse, parsedData);
            progress.RecordSuccess();
            
            logger.LogInformation(
                "[{Url}] ChatGPT MatchScore: [{MatchScore}], Remote: {Remote}, Summary: {Summary}",
                url, parsedData.MatchScore, parsedData.Remote, parsedData.Summary);

            return new JobProcessingResult
            {
                Success = true,
                Url = url,
                ParsedData = parsedData
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[{Url}] Error processing job", url);
            return new JobProcessingResult
            {
                Success = false,
                Url = url,
                ErrorMessage = ex.Message
            };
        }
    }
}
