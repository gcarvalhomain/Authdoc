using Microsoft.EntityFrameworkCore;

namespace Authdoc.Infrastructure.Persistence;

public class ManagerDbContext : DbContext
{
    public ManagerDbContext(DbContextOptions<ManagerDbContext> options) : base(options)
    {
        
    }
}