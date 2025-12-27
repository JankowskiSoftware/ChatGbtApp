using ChatGbtApp.Repository;
using ChatGgtApp.Crawler.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatGgtApp.Crawler.Storage;

public class JobStorage(AppDbContext dbContext, ILogger<JobStorage> logger, JobProcessingProgress progress)
{
    private static readonly object _lock = new();
    
    public bool IsDuplicate(string url)
    {
        lock (_lock)
        {
            var alreadyProcessed = dbContext.Jobs.Any(j => j.Url == url);
            if (alreadyProcessed)
            {
                logger.LogInformation($"[{url}] Duplicate detected; URL already in database.");
                return true;
            } 
        }
        
        return false;
    }
    
    public void Store(Job job)
    {
        try
        {
            lock (_lock)
            {
                dbContext.Add(job);
                dbContext.SaveChanges();
            }

            logger.LogInformation($"[{job.Url}] Successfully stored job listing.");
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            logger.LogInformation($"[{job.Url}] Duplicate detected during save.");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) == true;
}