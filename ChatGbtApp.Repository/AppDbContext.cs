using Microsoft.EntityFrameworkCore;
using System.IO;

namespace ChatGbtApp.Repository;

public class AppDbContext : DbContext
{
    public DbSet<Job> Jobs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Use solution root relative path
        var solutionRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..");
        var dataDirectory = Path.Combine(solutionRoot, "data");
        Directory.CreateDirectory(dataDirectory); // Ensure directory exists
        var dbPath = Path.Combine(dataDirectory, "jobs.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }
}