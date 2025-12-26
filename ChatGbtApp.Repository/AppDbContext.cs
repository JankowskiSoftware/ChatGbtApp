using Microsoft.EntityFrameworkCore;
using System.IO;

namespace ChatGbtApp.Repository;

public class AppDbContext : DbContext
{
    public DbSet<Job> Jobs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var solutionRoot = SolutionDirectory.GetRepoPath();
        
        var dataDirectory = Path.Combine(solutionRoot, "data");
        Directory.CreateDirectory(dataDirectory); // Ensure directory exists
        var dbPath = Path.Combine(dataDirectory, "jobs.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }
}