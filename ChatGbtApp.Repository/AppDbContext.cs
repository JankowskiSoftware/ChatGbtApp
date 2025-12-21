using Microsoft.EntityFrameworkCore;

namespace ChatGbtApp.Repository;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Job> Jobs { get; set; }
}