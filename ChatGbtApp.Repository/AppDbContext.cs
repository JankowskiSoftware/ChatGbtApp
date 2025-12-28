using Microsoft.EntityFrameworkCore;
using System.IO;

namespace ChatGbtApp.Repository;

public class AppDbContext : DbContext
{
    public DbSet<Job> Jobs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbPath = SolutionDirectory.GetRepoPath(Path.Combine("data", "jobs.db"));
        
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }
}