using System.Net;
using System.Net.Http.Json;
using Authdoc.Application.DTOs;
using Authdoc.Responses;

namespace Authdoc.Tests.Integration;

public class RefreshTokenTests : IClassFixture<AuthdocApiFactory>
{
    private readonly AuthdocApiFactory _factory;

    public RefreshTokenTests(AuthdocApiFactory factory)
    {
        _factory = factory;
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
        var client = _factory.CreateClient();
        var login = await client.LoginAsync(AuthdocApiFactory.AdminEmail, AuthdocApiFactory.AdminPassword);
        await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login.RefreshToken });

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login.RefreshToken });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("INVALID_REFRESH_TOKEN", error!.Code);
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
