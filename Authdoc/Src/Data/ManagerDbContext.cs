using Authdoc.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Authdoc.Data;

public class ManagerDbContext : DbContext
{
    public ManagerDbContext(DbContextOptions<ManagerDbContext> options) : base(options)
    {
    }

    public ManagerDbContext()
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            "Server=localhost\\SQLEXPRESS;Database=Authdoc;Trusted_Connection=True;TrustServerCertificate=True;");

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>().Property(u => u.CreatedAt).HasDefaultValueSql("getutcdate()");
    }
}