using Authdoc.Application.DTOs;
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
    public async Task ListAsync_WithPageTooBigForAnInt_ReturnsEmptyPage()
    {
        // Arrange
        using var context = CreateContext();
        context.Users.Add(new User { Name = "User", Email = "page-overflow@gmail.com" });
        await context.SaveChangesAsync();
        var service = new UserService(context);

        // Act
        // (page - 1) * pageSize does not fit in an int here.
        var result = await service.ListAsync(page: int.MaxValue, pageSize: 100);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(int.MaxValue, result.Page);
    }

    [Fact]
    public async Task IsTokenStillValidAsync_WhenUserHasSameRole_ReturnsTrue()
    {
        // Arrange
        using var context = CreateContext();
        var user = new User { Name = "User", Email = "token-same-role@gmail.com" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var service = new UserService(context);

        // Act
        var result = await service.IsTokenStillValidAsync(user.Id, "User", user.SecurityStamp.ToString());

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsTokenStillValidAsync_WhenSecurityStampChanged_ReturnsFalse()
    {
        // Arrange
        using var context = CreateContext();
        var user = new User { Name = "User", Email = "token-new-stamp@gmail.com" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var oldStamp = user.SecurityStamp.ToString();
        user.SecurityStamp = Guid.NewGuid();
        await context.SaveChangesAsync();
        var service = new UserService(context);

        // Act
        var result = await service.IsTokenStillValidAsync(user.Id, "User", oldStamp);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsTokenStillValidAsync_WhenRoleChanged_ReturnsFalse()
    {
        // Arrange
        using var context = CreateContext();
        var user = new User { Name = "User", Email = "token-new-role@gmail.com" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var service = new UserService(context);

        // Act
        var result = await service.IsTokenStillValidAsync(user.Id, "Admin", user.SecurityStamp.ToString());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsTokenStillValidAsync_WhenUserDoesNotExist_ReturnsFalse()
    {
        // Arrange
        using var context = CreateContext();
        var service = new UserService(context);

        // Act
        var result = await service.IsTokenStillValidAsync(Guid.NewGuid(), "User", Guid.NewGuid().ToString());

        // Assert
        Assert.False(result);
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

    [Fact]
    public async Task ChangeRoleAsync_WhenDemotingLastAdmin_ReturnsLastAdmin()
    {
        // Arrange
        using var context = CreateContext();
        var admin = new User { Name = "Admin", Email = "admin@gmail.com", Role = UserRole.Admin };
        context.Users.Add(admin);
        await context.SaveChangesAsync();
        var service = new UserService(context);

        // Act
        var (result, updatedUser) = await service.ChangeRoleAsync(admin.Id, UserRole.User);

        // Assert
        Assert.Equal(ChangeRoleResult.LastAdmin, result);
        Assert.Null(updatedUser);
        Assert.Equal(UserRole.Admin, admin.Role);
    }

    [Fact]
    public async Task ChangeRoleAsync_WhenPromotingUser_ReturnsSuccess()
    {
        // Arrange
        using var context = CreateContext();
        var admin = new User { Name = "Admin", Email = "admin@gmail.com", Role = UserRole.Admin };
        var user = new User { Name = "User", Email = "user@gmail.com" };
        context.Users.AddRange(admin, user);
        await context.SaveChangesAsync();
        var service = new UserService(context);

        // Act
        var (result, updatedUser) = await service.ChangeRoleAsync(user.Id, UserRole.Admin);

        // Assert
        Assert.Equal(ChangeRoleResult.Success, result);
        Assert.Equal(UserRole.Admin, user.Role);
        Assert.Equal(user.Id, updatedUser!.Id);
        Assert.Equal("Admin", updatedUser.Role);
    }

    [Fact]
    public async Task ChangeRoleAsync_WhenRoleIsTheSame_ReturnsSuccessWithUser()
    {
        // Arrange
        using var context = CreateContext();
        var user = new User { Name = "User", Email = "same-role@gmail.com" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var service = new UserService(context);

        // Act
        var (result, updatedUser) = await service.ChangeRoleAsync(user.Id, UserRole.User);

        // Assert
        Assert.Equal(ChangeRoleResult.Success, result);
        Assert.Equal("User", updatedUser!.Role);
    }

    [Fact]
    public async Task UpdateAsync_WhenUserExists_ReturnsUpdatedUser()
    {
        // Arrange
        using var context = CreateContext();
        var user = new User { Name = "Old Name", Email = "old-email@gmail.com" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var service = new UserService(context);

        // Act
        var (result, updatedUser) = await service.UpdateAsync(user.Id,
            new UpdateUserRequest { Name = "New Name", Email = "new-email@gmail.com" });

        // Assert
        Assert.Equal(UpdateUserResult.Updated, result);
        Assert.Equal(user.Id, updatedUser!.Id);
        Assert.Equal("New Name", updatedUser.Name);
        Assert.Equal("new-email@gmail.com", updatedUser.Email);
    }

    [Fact]
    public async Task UpdateAsync_WhenUserDoesNotExist_ReturnsUserNotFoundWithoutUser()
    {
        // Arrange
        using var context = CreateContext();
        var service = new UserService(context);

        // Act
        var (result, updatedUser) = await service.UpdateAsync(Guid.NewGuid(),
            new UpdateUserRequest { Name = "Nobody", Email = "nobody@gmail.com" });

        // Assert
        Assert.Equal(UpdateUserResult.UserNotFound, result);
        Assert.Null(updatedUser);
    }
}
