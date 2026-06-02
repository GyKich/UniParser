using Microsoft.EntityFrameworkCore;

namespace UniParser;

public class AppDbContext : DbContext
{
	public DbSet<TrackedProduct> Products { get; set; }
    public DbSet<UserMonitored> Subscriptions { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string dbPath = Path.Combine(AppContext.BaseDirectory, "botdata.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }
}