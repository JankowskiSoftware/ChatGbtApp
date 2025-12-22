using System.Security.Cryptography;
using System.Text;
using ChatGbtApp.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatGgtApp.Crawler;

public class JobStorage(AppDbContext dbContext,  ILogger<Chromium> logger)
{
    public async Task Store(string url, string message)
    {
        var hash = ComputeHash(message);

        if (string.IsNullOrWhiteSpace(hash))
        {
            logger.LogError($"Empty content for {url}; skipping store.");
            return;
        }

        if (await dbContext.Jobs.AnyAsync(j => j.Hash == hash))
        {
            logger.LogDebug($"Duplicate content detected for {url}; already stored.");
            return;
        }

        dbContext.Add(new Job
        {
            DateTime = DateTime.Now,
            Title = "Title",
            Company = "Company",
            Score = 3,
            FileLocation = url,
            Hash = hash,
            JobDescription = message
        });

        await dbContext.SaveChangesAsync();
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