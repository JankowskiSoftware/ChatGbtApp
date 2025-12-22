using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using ChatGbtApp;
using ChatGbtApp.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatGgtApp.Crawler;

public class JobStorage
{
    public enum JobStorageStatus
    {
        Success,
        Duplicate, 
        EmptyJobDescription
    }

    private readonly AppDbContext _dbContext;
    private readonly ILogger<Chromium> _logger;
    private readonly string _resultsDir;
    private readonly IMapper _mapper;

    public JobStorage(AppDbContext dbContext,  IMapper mapper, ILogger<Chromium> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;

        _resultsDir = Path.Combine("results", SolutionDirectory.FindRepoRoot());
    }


    public async Task<JobStorageStatus> Store(string url, string jobDescription, string message, ParsedJobFit values)
    {
        var hash = ComputeHash(message);

        if (string.IsNullOrWhiteSpace(hash))
        {
            _logger.LogError($"{url}: Empty content for {url}; skipping store.");
            return JobStorageStatus.EmptyJobDescription;
        }

        if (await _dbContext.Jobs.AnyAsync(j => j.Hash == hash))
            if (await _dbContext.Jobs.AnyAsync(j => j.JobDescription == jobDescription))
            {
                _logger.LogDebug($"{url}: Duplicate content detected for {url}; already stored.");
                return JobStorageStatus.Duplicate;
            }

        var baseJob = new JobBase
        {
            Url = url,
            DateTime = DateTime.Now,
            JobDescription = jobDescription,
            Hash = hash,
            Message = message,
        };
        
        var job = _mapper.Map<Job>(baseJob);
        job = _mapper.Map<Job>(values);

    
        _dbContext.Add(job);

        await _dbContext.SaveChangesAsync();
        
        return JobStorageStatus.Success;
    }
    
    

    private static string ComputeHash(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        var normalized = content.Trim();
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hashBytes = sha.ComputeHash(bytes);
        var sb = new StringBuilder(hashBytes.Length * 2);
        foreach (var b in hashBytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}