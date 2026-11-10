using Authdoc.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Authdoc.Data;

public class ManagerDbContext : DbContext
{
    public ManagerDbContext(DbContextOptions<ManagerDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>().Property(u => u.CreatedAt).HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Name)
                .HasMaxLength(User.NameMaxLength)
                .IsRequired();

            entity.Property(u => u.Email)
                .HasMaxLength(254)
                .IsRequired();

            entity.HasIndex(u => u.Email)
                .IsUnique();
        });
        modelBuilder.Entity<Document>(entity =>
        {
            entity.Property(d => d.Type)
                .HasMaxLength(Document.TypeMaxLength)
                .IsRequired();

            entity.Property(d => d.Name)
                .HasMaxLength(Document.NameMaxLength)
                .IsRequired();

            entity.Property(d => d.Number)
                .HasMaxLength(Document.NumberMaxLength)
                .IsRequired();

            entity.Property(d => d.Description)
                .HasMaxLength(Document.DescriptionMaxLength);

            entity.HasOne(d => d.Owner)
                .WithMany()
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.Property(t => t.TokenHash)
                .HasMaxLength(64)
                .IsRequired();

            entity.HasIndex(t => t.TokenHash)
                .IsUnique();

            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}