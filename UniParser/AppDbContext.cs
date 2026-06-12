using Microsoft.EntityFrameworkCore;

namespace UniParser;

public class AppDbContext : DbContext
{
	public DbSet<TrackedProduct> Products { get; set; }
    public DbSet<UserMonitored> Subscriptions { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source=data/botdata.db");
    }
}