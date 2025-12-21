using Microsoft.EntityFrameworkCore;
using System.IO;

namespace ChatGbtApp.Repository;

public class AppDbContext : DbContext
{
    public DbSet<Job> Jobs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "data");
        Directory.CreateDirectory(dataDirectory); // Ensure directory exists
        var dbPath = Path.Combine(dataDirectory, "jobs.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }
}