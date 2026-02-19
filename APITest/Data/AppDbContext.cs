using APITest.Models;
using Microsoft.EntityFrameworkCore;

namespace APITest.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Login> Logins => Set<Login>();

    public DbSet<CcUser> Users => Set<CcUser>();
    public DbSet<Area> Areas => Set<Area>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CcUser>().ToTable("ccUsers", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<CcUser>().HasKey(u => u.UserId);

        modelBuilder.Entity<Area>().ToTable("ccRIACat_Areas", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<Area>().HasKey(a => a.IDArea);

        modelBuilder.Entity<Login>().ToTable("ccloglogin");
        modelBuilder.Entity<Login>().HasKey(l => l.LogId);
    }
}
