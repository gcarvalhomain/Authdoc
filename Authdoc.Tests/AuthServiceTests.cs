using Authdoc.Application.DTOs;
using Authdoc.Application.Services;
using Authdoc.Data;
using Authdoc.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Authdoc.Tests;

public class AuthServiceTests
{
    private static readonly string TooLongPassword = new('a', User.PasswordMaxLength + 1);

    // Two contexts with the same database name act like two requests at the same time.
    private static ManagerDbContext CreateContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ManagerDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
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
        var service = new AuthService(context, hasher, new ConfigurationBuilder().Build(), NullLogger<AuthService>.Instance);

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
        var service = new AuthService(context, hasher, new ConfigurationBuilder().Build(), NullLogger<AuthService>.Instance);

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

    [Fact]
    public async Task RefreshAsync_WhenAnotherRequestUsedTheTokenFirst_ReturnsNull()
    {
        // Arrange
        var databaseName = Guid.NewGuid().ToString();
        using var firstContext = CreateContext(databaseName);
        using var secondContext = CreateContext(databaseName);
        await AddUserAsync(firstContext, "refresh-race@gmail.com");
        var firstService = CreateServiceWithJwt(firstContext);
        var secondService = CreateServiceWithJwt(secondContext);
        var login = await firstService.LoginAsync(new LoginRequest { Email = "refresh-race@gmail.com", Password = "senha123" });
        // The second request reads the token while it is still active, before the first one saves.
        await secondContext.RefreshTokens.Include(token => token.User).SingleAsync();
        var firstRefresh = await firstService.RefreshAsync(login!.RefreshToken);

        // Act
        var secondRefresh = await secondService.RefreshAsync(login.RefreshToken);

        // Assert
        Assert.NotNull(firstRefresh);
        Assert.Null(secondRefresh);
        using var checkContext = CreateContext(databaseName);
        Assert.Equal(2, await checkContext.RefreshTokens.CountAsync());
    }

    [Fact]
    public async Task LogoutAsync_WhenAnotherRequestRevokedTheTokenFirst_DoesNotThrow()
    {
        // Arrange
        var databaseName = Guid.NewGuid().ToString();
        using var firstContext = CreateContext(databaseName);
        using var secondContext = CreateContext(databaseName);
        await AddUserAsync(firstContext, "logout-race@gmail.com");
        var firstService = CreateServiceWithJwt(firstContext);
        var secondService = CreateServiceWithJwt(secondContext);
        var login = await firstService.LoginAsync(new LoginRequest { Email = "logout-race@gmail.com", Password = "senha123" });
        await secondContext.RefreshTokens.SingleAsync();
        await firstService.LogoutAsync(login!.RefreshToken);

        // Act
        var exception = await Record.ExceptionAsync(() => secondService.LogoutAsync(login.RefreshToken));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task RefreshAsync_WithRotatedToken_RevokesEverySessionAndChangesStamp()
    {
        // Arrange
        using var context = CreateContext();
        var user = await AddUserAsync(context, "reuse-rotated@gmail.com");
        var service = CreateServiceWithJwt(context);
        var stolen = await service.LoginAsync(new LoginRequest { Email = "reuse-rotated@gmail.com", Password = "senha123" });
        await service.LoginAsync(new LoginRequest { Email = "reuse-rotated@gmail.com", Password = "senha123" });
        // The attacker uses the stolen token first and gets a new session.
        var attackerSession = await service.RefreshAsync(stolen!.RefreshToken);
        await MoveRotationsBeforeGracePeriodAsync(context);
        var stampBefore = user.SecurityStamp;

        // Act
        // The real user sends the same token again, after the grace period.
        var result = await service.RefreshAsync(stolen.RefreshToken);

        // Assert
        // 3 tokens: the reused one, the attacker's new one and the other session's one.
        Assert.NotNull(attackerSession);
        Assert.Null(result);
        Assert.All(await context.RefreshTokens.ToListAsync(), token => Assert.NotNull(token.RevokedAt));
        Assert.Equal(3, await context.RefreshTokens.CountAsync(token => token.RevokeReason == RefreshTokenRevokeReason.ReuseDetected));
        Assert.NotEqual(stampBefore, user.SecurityStamp);
    }

    [Fact]
    public async Task RefreshAsync_WithRotatedTokenInsideGracePeriod_ReturnsNullAndKeepsSessions()
    {
        // Arrange
        using var context = CreateContext();
        var user = await AddUserAsync(context, "reuse-grace@gmail.com");
        var service = CreateServiceWithJwt(context);
        var login = await service.LoginAsync(new LoginRequest { Email = "reuse-grace@gmail.com", Password = "senha123" });
        // A first tab refreshes...
        var firstTab = await service.RefreshAsync(login!.RefreshToken);
        var stampBefore = user.SecurityStamp;

        // Act
        // ...and a second tab sends the same token a moment later.
        var secondTab = await service.RefreshAsync(login.RefreshToken);

        // Assert
        Assert.NotNull(firstTab);
        Assert.Null(secondTab);
        Assert.Equal(1, await context.RefreshTokens.CountAsync(token => token.RevokedAt == null));
        Assert.Equal(stampBefore, user.SecurityStamp);
    }

    [Fact]
    public async Task RefreshAsync_WithExpiredRotatedToken_ReturnsNullAndKeepsSessions()
    {
        // Arrange
        using var context = CreateContext();
        var user = await AddUserAsync(context, "reuse-expired-rotated@gmail.com");
        var service = CreateServiceWithJwt(context);
        var old = await service.LoginAsync(new LoginRequest { Email = "reuse-expired-rotated@gmail.com", Password = "senha123" });
        await service.RefreshAsync(old!.RefreshToken);
        await MoveRotationsBeforeGracePeriodAsync(context);
        var rotated = await context.RefreshTokens.SingleAsync(token => token.RevokeReason == RefreshTokenRevokeReason.Rotated);
        rotated.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();
        var stampBefore = user.SecurityStamp;

        // Act
        var result = await service.RefreshAsync(old.RefreshToken);

        // Assert
        Assert.Null(result);
        Assert.Equal(1, await context.RefreshTokens.CountAsync(token => token.RevokedAt == null));
        Assert.Equal(stampBefore, user.SecurityStamp);
    }

    [Fact]
    public async Task RefreshAsync_WithSameRotatedTokenTwice_EndsSessionsOnlyOnce()
    {
        // Arrange
        using var context = CreateContext();
        var user = await AddUserAsync(context, "reuse-twice@gmail.com");
        var service = CreateServiceWithJwt(context);
        var stolen = await service.LoginAsync(new LoginRequest { Email = "reuse-twice@gmail.com", Password = "senha123" });
        await service.RefreshAsync(stolen!.RefreshToken);
        await MoveRotationsBeforeGracePeriodAsync(context);
        await service.RefreshAsync(stolen.RefreshToken);
        // After the alarm, the user signs in again.
        await service.LoginAsync(new LoginRequest { Email = "reuse-twice@gmail.com", Password = "senha123" });
        var stampBefore = user.SecurityStamp;

        // Act
        var result = await service.RefreshAsync(stolen.RefreshToken);

        // Assert
        Assert.Null(result);
        Assert.Equal(1, await context.RefreshTokens.CountAsync(token => token.RevokedAt == null));
        Assert.Equal(stampBefore, user.SecurityStamp);
    }

    // Moves the rotation of every rotated token one minute back, after the grace period,
    // as if the token came back later.
    private static async Task MoveRotationsBeforeGracePeriodAsync(ManagerDbContext context)
    {
        var rotated = await context.RefreshTokens
            .Where(token => token.RevokeReason == RefreshTokenRevokeReason.Rotated)
            .ToListAsync();

        foreach (var token in rotated)
        {
            token.RevokedAt = token.RevokedAt!.Value.AddMinutes(-1);
        }

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task RefreshAsync_WithExpiredToken_ReturnsNullAndKeepsOtherSessions()
    {
        // Arrange
        using var context = CreateContext();
        var user = await AddUserAsync(context, "reuse-expired@gmail.com");
        var service = CreateServiceWithJwt(context);
        var expired = await service.LoginAsync(new LoginRequest { Email = "reuse-expired@gmail.com", Password = "senha123" });
        var expiredToken = await context.RefreshTokens.SingleAsync();
        expiredToken.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();
        await service.LoginAsync(new LoginRequest { Email = "reuse-expired@gmail.com", Password = "senha123" });
        var stampBefore = user.SecurityStamp;

        // Act
        var result = await service.RefreshAsync(expired!.RefreshToken);

        // Assert
        // An expired token is only invalid: nothing is revoked, not even the expired token itself.
        Assert.Null(result);
        Assert.Equal(0, await context.RefreshTokens.CountAsync(token => token.RevokedAt != null));
        Assert.Equal(stampBefore, user.SecurityStamp);
    }

    [Fact]
    public async Task RefreshAsync_WithTokenRevokedByLogout_ReturnsNullAndKeepsOtherSessions()
    {
        // Arrange
        using var context = CreateContext();
        var user = await AddUserAsync(context, "reuse-logout@gmail.com");
        var service = CreateServiceWithJwt(context);
        var loggedOut = await service.LoginAsync(new LoginRequest { Email = "reuse-logout@gmail.com", Password = "senha123" });
        await service.LoginAsync(new LoginRequest { Email = "reuse-logout@gmail.com", Password = "senha123" });
        await service.LogoutAsync(loggedOut!.RefreshToken);
        var stampBefore = user.SecurityStamp;

        // Act
        var result = await service.RefreshAsync(loggedOut.RefreshToken);

        // Assert
        Assert.Null(result);
        Assert.Equal(1, await context.RefreshTokens.CountAsync(token => token.RevokedAt == null));
        Assert.Equal(stampBefore, user.SecurityStamp);
    }

    [Fact]
    public async Task RefreshAsync_WithTokenRevokedByPasswordChange_KeepsTheNewSession()
    {
        // Arrange
        using var context = CreateContext();
        var user = await AddUserAsync(context, "reuse-password@gmail.com");
        var service = CreateServiceWithJwt(context);
        // An old device keeps this token, and the user changes the password on another device.
        var oldDevice = await service.LoginAsync(new LoginRequest { Email = "reuse-password@gmail.com", Password = "senha123" });
        await service.ChangePasswordAsync(user.Id, new ChangePasswordRequest
        {
            CurrentPassword = "senha123",
            NewPassword = "novaSenha456",
            ConfirmationPassword = "novaSenha456"
        });
        await service.LoginAsync(new LoginRequest { Email = "reuse-password@gmail.com", Password = "novaSenha456" });
        var stampBefore = user.SecurityStamp;

        // Act
        var result = await service.RefreshAsync(oldDevice!.RefreshToken);

        // Assert
        Assert.Null(result);
        Assert.Equal(1, await context.RefreshTokens.CountAsync(token => token.RevokedAt == null));
        Assert.Equal(stampBefore, user.SecurityStamp);
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

        return new AuthService(context, new PasswordHasher<User>(), configuration, NullLogger<AuthService>.Instance);
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
