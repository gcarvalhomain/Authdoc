using Authdoc.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Authdoc.Data;

public class ManagerDbContext : DbContext
{
    public ManagerDbContext(DbContextOptions<ManagerDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>().Property(u => u.CreatedAt).HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Email)
                .HasMaxLength(254)
                .IsRequired();
            
            entity.HasIndex(u => u.Email)
                .IsUnique();
        });
    }
}