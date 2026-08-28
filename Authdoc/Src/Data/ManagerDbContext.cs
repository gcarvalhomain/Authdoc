using Authdoc.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Authdoc.Data;

public class ManagerDbContext : DbContext
{
    public ManagerDbContext(DbContextOptions<ManagerDbContext> options) : base(options)
    {
    }
    public ManagerDbContext ()
    {
    }
    public DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Server=localhost;Database=AuthdocDb;Trusted_Connection=True;TrustServerCertificate=True;;");
    }
}