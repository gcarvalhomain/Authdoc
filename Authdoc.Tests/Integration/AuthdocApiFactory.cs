using Authdoc.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Authdoc.Tests.Integration;

public class AuthdocApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@system.com";
    public const string AdminPassword = "Test-Admin-Password-123";

    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected virtual int LoginPermitLimit => 1000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Key", "integration-tests-signing-key-with-at-least-32-bytes");
        builder.UseSetting("SeedData:AdminPassword", AdminPassword);
        builder.UseSetting("RateLimiting:LoginPermitLimit", LoginPermitLimit.ToString());

        builder.ConfigureServices(services =>
        {
            var sqlServerRegistrations = services
                .Where(service =>
                    service.ServiceType == typeof(DbContextOptions<ManagerDbContext>) ||
                    service.ServiceType == typeof(IDbContextOptionsConfiguration<ManagerDbContext>))
                .ToList();

            foreach (var registration in sqlServerRegistrations)
            {
                services.Remove(registration);
            }

            services.AddDbContext<ManagerDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}

public class RateLimitedApiFactory : AuthdocApiFactory
{
    protected override int LoginPermitLimit => 5;
}
