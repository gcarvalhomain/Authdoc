using Authdoc.Data;
using Authdoc.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Authdoc.Tests;

public class DbInitializerTests
{
    private const string AdminEmail = "admin@system.com";
    private const string AdminPassword = "Test-Admin-Password-123";

    private static ManagerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ManagerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ManagerDbContext(options);
    }

    [Fact]
    public async Task SeedAdminAsync_WithEmptyDatabase_CreatesAdmin()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        await DbInitializer.SeedAdminAsync(context, new PasswordHasher<User>(), AdminPassword);

        // Assert
        var admin = await context.Users.SingleAsync();
        Assert.Equal(AdminEmail, admin.Email);
        Assert.Equal(UserRole.Admin, admin.Role);
    }

    [Fact]
    public async Task SeedAdminAsync_WhenAdminEmailBelongsToNormalUser_ThrowsAndKeepsUserRole()
    {
        // Arrange
        using var context = CreateContext();
        var user = new User { Name = "Not an admin", Email = AdminEmail };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => DbInitializer.SeedAdminAsync(context, new PasswordHasher<User>(), AdminPassword));

        // Assert
        Assert.Contains(AdminEmail, exception.Message);
        Assert.Equal(UserRole.User, user.Role);
    }

    [Fact]
    public async Task SeedAdminAsync_WhenAnAdminExists_DoesNotCreateAnotherUser()
    {
        // Arrange
        using var context = CreateContext();
        context.Users.Add(new User { Name = "Admin", Email = "owner@gmail.com", Role = UserRole.Admin });
        await context.SaveChangesAsync();

        // Act
        await DbInitializer.SeedAdminAsync(context, new PasswordHasher<User>(), AdminPassword);

        // Assert
        Assert.Equal(1, await context.Users.CountAsync());
    }
}
