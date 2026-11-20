using System.Net;
using System.Net.Http.Json;
using Authdoc.Responses;

namespace Authdoc.Tests.Integration;

public class LogoutTests : IClassFixture<AuthdocApiFactory>
{
    private readonly AuthdocApiFactory _factory;

    public LogoutTests(AuthdocApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Logout_WithValidToken_Returns204AndTheTokenCannotRefresh()
    {
        // Arrange
        var client = _factory.CreateClient();
        var login = await client.LoginAsync(AuthdocApiFactory.AdminEmail, AuthdocApiFactory.AdminPassword);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = login.RefreshToken });

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        var error = await refresh.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("INVALID_REFRESH_TOKEN", error!.Code);
    }

    [Fact]
    public async Task Logout_WithUnknownToken_Returns204()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = "not-a-real-token" });

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutToken_Returns400RefreshTokenRequired()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = "  " });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("REFRESH_TOKEN_REQUIRED", error!.Code);
    }

    [Fact]
    public async Task Logout_KeepsTheOtherSessionsActive()
    {
        // Arrange: two sign-ins of the same user, like a laptop and a phone.
        var client = _factory.CreateClient();
        var laptop = await client.LoginAsync(AuthdocApiFactory.AdminEmail, AuthdocApiFactory.AdminPassword);
        var phone = await client.LoginAsync(AuthdocApiFactory.AdminEmail, AuthdocApiFactory.AdminPassword);

        // Act
        await client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = laptop.RefreshToken });

        // Assert
        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = phone.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
    }
}
