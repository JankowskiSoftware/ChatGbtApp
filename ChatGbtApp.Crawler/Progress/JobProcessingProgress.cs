using Microsoft.Extensions.Logging;

namespace ChatGgtApp.Crawler.Progress;

public class JobProcessingProgress
{
    private readonly ILogger<JobProcessingProgress> _logger;
    
    private int _totalProcessed = 0;
    private int _successCount = 0;
    private int _duplicateCount = 0;
    private int _emptyCount = 0;
    private int _total;

    public JobProcessingProgress(ILogger<JobProcessingProgress> logger)
    {
        _logger = logger;
    }
    
    public void RecordSuccess()
    {
        _totalProcessed++;
        _successCount++;
    }
    
    public void RecordDuplicate()
    {
        _totalProcessed++;
        _duplicateCount++;
    }
    
    public void RecordEmpty()
    {
        _totalProcessed++;
        _emptyCount++;
    }
    
    public void LogProgress()
    {
        _logger.LogInformation(
            $"[Progress] Completed: {_totalProcessed}/{_total} |  Stored: {_successCount} |  Duplicates: {_duplicateCount} |  Empty: {_emptyCount}"
        );
        _logger.LogInformation("");
    }
    
    public void Reset(int linksLength)
    {
        _total = linksLength;
        _totalProcessed = 0;
        _successCount = 0;
        _duplicateCount = 0;
        _emptyCount = 0;
    }
    
    public void PrintSummary()
    {
        var successRate = _totalProcessed > 0 ? (_successCount * 100.0 / _totalProcessed) : 0;
        var divider = new string('═', 70);
        
        _logger.LogInformation($"\n{divider}");
        _logger.LogInformation($"    CRAWL SUMMARY");
        _logger.LogInformation($"{divider}");
        _logger.LogInformation($"  Total Processed:    {_totalProcessed}");
        _logger.LogInformation($"    Successfully Stored: {_successCount} ({successRate:F1}%)");
        _logger.LogInformation($"    Duplicates:       {_duplicateCount}");
        _logger.LogInformation($"    Empty Pages:      {_emptyCount}");
        _logger.LogInformation($"{divider}\n");
    }
}
