using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Authdoc.Data;

public class ManagerDbContextFactory : IDesignTimeDbContextFactory<ManagerDbContext>
{
    public ManagerDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ManagerDbContext>()
            .UseSqlServer("Server=localhost;Database=AuthdocDb;Trusted_Connection=True;TrustServerCertificate=True;;")
            .Options;

        return new ManagerDbContext(options);
    }
}