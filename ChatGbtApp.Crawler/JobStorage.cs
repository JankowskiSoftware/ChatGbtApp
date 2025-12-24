using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using ChatGbtApp;
using ChatGbtApp.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatGgtApp.Crawler;

public class JobStorage(AppDbContext dbContext, IMapper mapper, ILogger<Chromium> logger, JobProcessingProgress progress)
{

    public async Task<bool> IsDuplicate(string url)
    {
        lock (this)
        {
            var alreadyProcessed = dbContext.Jobs.Any(j => j.Url == url);
            if (alreadyProcessed)
            {
                logger.LogInformation($"[{url}] Duplicate detected; URL already in database.");
                progress.RecordDuplicate();
                return true;
            } 
        }
        
        return false;
    }
    
    public async Task Store(string url, string jobDescription, string message, ParsedJobFit values)
    {
        var baseJob = new JobBase
        {
            Url = url,
            DateTime = DateTime.Now,
            JobDescription = jobDescription,
            Message = message,
        };
        
        var job = mapper.Map<Job>(baseJob);
        mapper.Map(values, job);

        lock (this)
        {
            dbContext.Add(job);
            dbContext.SaveChanges();
        }

        
        progress.RecordSuccess();
        logger.LogInformation($"[{url}] Successfully stored job listing.");
    }
}