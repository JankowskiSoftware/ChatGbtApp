using ChatGbtApp.Repository;
using ChatGgtApp.Crawler.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatGgtApp.Crawler.Storage;

public class JobStorage(IServiceScopeFactory scopeFactory, ILogger<JobStorage> logger, JobProcessingProgress progress)
{
    private static readonly object _lock = new();
    
    public bool IsDuplicate(string url)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        lock (_lock)
        {
            var alreadyProcessed = dbContext.Jobs.Any(j => j.Url == url);
            if (alreadyProcessed)
            {
                logger.LogDebug($"[{url}] Duplicate detected; URL already in database.");
                return true;
            } 
        }
        
        return false;
    }
    
    public void Store(Job job)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        try
        {
            lock (_lock)
            {
                dbContext.Add(job);
                dbContext.SaveChanges();
            }

            logger.LogDebug($"[{job.Url}] Successfully stored job listing.");
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            logger.LogError($"[{job.Url}] Duplicate detected during save.");
        }
    }
    
    

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) == true;
}