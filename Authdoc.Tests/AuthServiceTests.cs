using Authdoc.Application.DTOs;
using Authdoc.Application.Services;
using Authdoc.Data;
using Authdoc.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Authdoc.Tests;

public class AuthServiceTests
{
    private static readonly string TooLongPassword = new('a', User.PasswordMaxLength + 1);

    private static ManagerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ManagerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ManagerDbContext(options);
    }

    private static async Task<User> AddUserAsync(ManagerDbContext context, string email)
    {
        var user = new User { Name = "User", Email = email };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "senha123");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task LoginAsync_WithTooLongPassword_ReturnsNullWithoutHashing()
    {
        // Arrange
        using var context = CreateContext();
        await AddUserAsync(context, "login-too-long@gmail.com");
        var hasher = new CountingPasswordHasher();
        var service = new AuthService(context, hasher, new ConfigurationBuilder().Build());

        // Act
        var result = await service.LoginAsync(new LoginRequest
        {
            Email = "login-too-long@gmail.com",
            Password = TooLongPassword
        });

        // Assert
        Assert.Null(result);
        Assert.Equal(0, hasher.Calls);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithTooLongCurrentPassword_ReturnsWrongCurrentPasswordWithoutHashing()
    {
        // Arrange
        using var context = CreateContext();
        var user = await AddUserAsync(context, "change-too-long@gmail.com");
        var hasher = new CountingPasswordHasher();
        var service = new AuthService(context, hasher, new ConfigurationBuilder().Build());

        // Act
        var result = await service.ChangePasswordAsync(user.Id, new ChangePasswordRequest
        {
            CurrentPassword = TooLongPassword,
            NewPassword = "novaSenha456",
            ConfirmationPassword = "novaSenha456"
        });

        // Assert
        Assert.Equal(ChangePasswordResult.WrongCurrentPassword, result);
        Assert.Equal(0, hasher.Calls);
    }

    [Fact]
    public async Task LogoutAsync_WithActiveToken_RevokesIt()
    {
        // Arrange
        using var context = CreateContext();
        await AddUserAsync(context, "logout-active@gmail.com");
        var service = CreateServiceWithJwt(context);
        var login = await service.LoginAsync(new LoginRequest { Email = "logout-active@gmail.com", Password = "senha123" });

        // Act
        await service.LogoutAsync(login!.RefreshToken);

        // Assert
        var storedToken = await context.RefreshTokens.SingleAsync();
        Assert.NotNull(storedToken.RevokedAt);
    }

    [Fact]
    public async Task LogoutAsync_WithAlreadyRevokedToken_KeepsTheFirstRevokedAt()
    {
        // Arrange
        using var context = CreateContext();
        await AddUserAsync(context, "logout-twice@gmail.com");
        var service = CreateServiceWithJwt(context);
        var login = await service.LoginAsync(new LoginRequest { Email = "logout-twice@gmail.com", Password = "senha123" });
        await service.LogoutAsync(login!.RefreshToken);
        var firstRevokedAt = (await context.RefreshTokens.SingleAsync()).RevokedAt;

        // Act
        await service.LogoutAsync(login.RefreshToken);

        // Assert
        var storedToken = await context.RefreshTokens.SingleAsync();
        Assert.Equal(firstRevokedAt, storedToken.RevokedAt);
    }

    // LoginAsync creates a JWT, so these tests need the Jwt settings.
    private static AuthService CreateServiceWithJwt(ManagerDbContext context)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "a-test-signing-key-with-at-least-32-bytes",
                ["Jwt:Issuer"] = "Authdoc",
                ["Jwt:Audience"] = "Users",
                ["Jwt:ExpirationInMinutes"] = "60"
            })
            .Build();

        return new AuthService(context, new PasswordHasher<User>(), configuration);
    }

    // Counts every hash and verify, to prove that a too long password is never hashed.
    private sealed class CountingPasswordHasher : IPasswordHasher<User>
    {
        private readonly PasswordHasher<User> _inner = new();

        public int Calls { get; private set; }

        public string HashPassword(User user, string password)
        {
            Calls++;
            return _inner.HashPassword(user, password);
        }

        public PasswordVerificationResult VerifyHashedPassword(User user, string hashedPassword, string providedPassword)
        {
            Calls++;
            return _inner.VerifyHashedPassword(user, hashedPassword, providedPassword);
        }
    }
}
