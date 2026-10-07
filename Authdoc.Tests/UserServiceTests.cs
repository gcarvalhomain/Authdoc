using Authdoc.Application.Services;
using Authdoc.Data;
using Authdoc.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Authdoc.Tests;

public class UserServiceTests
{
    private static ManagerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ManagerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ManagerDbContext(options);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserDoesNotExist_ReturnsUserNotFound()
    {
        // Arrange
        using var context = CreateContext();
        var service = new UserService(context);

        // Act
        var result = await service.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.Equal(DeleteUserResult.UserNotFound, result);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserIsLastAdmin_ReturnsLastAdmin()
    {
        // Arrange
        using var context = CreateContext();
        var admin = new User { Name = "Admin", Email = "gabriel@outlook.com", Role = UserRole.Admin };
        context.Users.Add(admin);
        await context.SaveChangesAsync();
        var service = new UserService(context);

        // Act
        var result = await service.DeleteAsync(admin.Id);

        // Assert
        Assert.Equal(DeleteUserResult.LastAdmin, result);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserIsNormalUser_ReturnsDeleted()
    {
        // Arrange
        using var context = CreateContext();
        var admin = new User { Name = "Admin", Email = "admin@gmail.com", Role = UserRole.Admin };
        var user = new User { Name = "User", Email = "user@gmail.com" };
        context.Users.AddRange(admin, user);
        await context.SaveChangesAsync();
        var service = new UserService(context);

        // Act
        var result = await service.DeleteAsync(user.Id);

        // Assert
        Assert.Equal(DeleteUserResult.Deleted, result);
        Assert.Null(await context.Users.FindAsync(user.Id));
    }
}
