using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Authdoc.Application.DTOs;
using Authdoc.Data;
using Authdoc.Models.Entities;
using Authdoc.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Authdoc.Tests.Integration;

public class RefreshTokenTests : IClassFixture<AuthdocApiFactory>
{
    private readonly AuthdocApiFactory _factory;

    public RefreshTokenTests(AuthdocApiFactory factory)
    {
        _factory = factory;
    }

    // Moves the rotation of the user's rotated tokens one minute back, after the grace period,
    // as if the token came back later.
    private async Task MoveRotationsBeforeGracePeriodAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ManagerDbContext>();
        var rotated = await context.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokeReason == RefreshTokenRevokeReason.Rotated)
            .ToListAsync();

        foreach (var token in rotated)
        {
            token.RevokedAt = token.RevokedAt!.Value.AddMinutes(-1);
        }

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewTokens()
    {
        // Arrange
        var client = _factory.CreateClient();
        var login = await client.LoginAsync(AuthdocApiFactory.AdminEmail, AuthdocApiFactory.AdminPassword);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login.RefreshToken });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var refreshed = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(refreshed!.Token));
        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
    }

    [Fact]
    public async Task Refresh_WithAlreadyUsedToken_Returns401InvalidRefreshToken()
    {
        // Arrange
        // Its own user: a reused token ends every session of the user, so the admin is not used here.
        var adminClient = await _factory.CreateAdminClientAsync();
        await adminClient.RegisterUserAsync("refresh-used@gmail.com");
        var client = _factory.CreateClient();
        var login = await client.LoginAsync("refresh-used@gmail.com", "senha123");
        await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login.RefreshToken });

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login.RefreshToken });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("INVALID_REFRESH_TOKEN", error!.Code);
    }

    [Fact]
    public async Task Refresh_WithAlreadyUsedToken_EndsEverySessionOfTheUser()
    {
        // Arrange
        var adminClient = await _factory.CreateAdminClientAsync();
        await adminClient.RegisterUserAsync("refresh-reuse@gmail.com");
        var client = _factory.CreateClient();
        var otherDevice = await client.LoginAsync("refresh-reuse@gmail.com", "senha123");
        var stolen = await client.LoginAsync("refresh-reuse@gmail.com", "senha123");
        // The attacker uses the stolen token first and gets a new pair of tokens.
        var attackerResponse = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = stolen.RefreshToken });
        var attacker = (await attackerResponse.Content.ReadFromJsonAsync<LoginResponse>())!;
        await MoveRotationsBeforeGracePeriodAsync(attacker.User.Id);

        // Act
        // The real user sends the same token again, after the grace period.
        var reuse = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = stolen.RefreshToken });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        var attackerRefresh = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = attacker.RefreshToken });
        var otherDeviceRefresh = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = otherDevice.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, attackerRefresh.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, otherDeviceRefresh.StatusCode);

        var attackerClient = _factory.CreateClient();
        attackerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", attacker.Token);
        var me = await attackerClient.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_Returns401InvalidRefreshToken()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = "not-a-real-token" });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("INVALID_REFRESH_TOKEN", error!.Code);
    }

    [Fact]
    public async Task Refresh_AfterRoleChange_ReturnsTokenWithNewRole()
    {
        // Arrange
        var adminClient = await _factory.CreateAdminClientAsync();
        var user = await adminClient.RegisterUserAsync("promoted@gmail.com");
        var userClient = _factory.CreateClient();
        var userLogin = await userClient.LoginAsync("promoted@gmail.com", "senha123");
        await adminClient.PatchAsJsonAsync($"/api/users/{user.Id}/role", new { role = "Admin" });

        // Act
        var response = await userClient.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = userLogin.RefreshToken });

        // Assert
        var refreshed = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.Equal("User", userLogin.User.Role);
        Assert.Equal("Admin", refreshed!.User.Role);
    }
}
