using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Authdoc.Responses;

namespace Authdoc.Tests.Integration;

public class ChangePasswordTests : IClassFixture<AuthdocApiFactory>
{
    private readonly AuthdocApiFactory _factory;

    public ChangePasswordTests(AuthdocApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, string RefreshToken)> CreateUserClientAsync(string email)
    {
        var adminClient = await _factory.CreateAdminClientAsync();
        await adminClient.RegisterUserAsync(email, "senha123");

        var client = _factory.CreateClient();
        var login = await client.LoginAsync(email, "senha123");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        return (client, login.RefreshToken);
    }

    [Fact]
    public async Task ChangePassword_WithCorrectCurrentPassword_Returns204AndNewPasswordWorks()
    {
        // Arrange
        var (client, _) = await CreateUserClientAsync("change1@gmail.com");

        // Act
        var response = await client.PutAsJsonAsync("/api/auth/password", new
        {
            currentPassword = "senha123",
            newPassword = "novaSenha456",
            confirmationPassword = "novaSenha456"
        });

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var anonymous = _factory.CreateClient();
        var oldLogin = await anonymous.PostAsJsonAsync("/api/auth/login", new { email = "change1@gmail.com", password = "senha123" });
        var newLogin = await anonymous.PostAsJsonAsync("/api/auth/login", new { email = "change1@gmail.com", password = "novaSenha456" });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_Returns400CurrentPasswordIncorrect()
    {
        // Arrange
        var (client, _) = await CreateUserClientAsync("change2@gmail.com");

        // Act
        var response = await client.PutAsJsonAsync("/api/auth/password", new
        {
            currentPassword = "wrong-password",
            newPassword = "novaSenha456",
            confirmationPassword = "novaSenha456"
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("CURRENT_PASSWORD_INCORRECT", error!.Code);
    }

    [Fact]
    public async Task ChangePassword_RevokesExistingRefreshTokens()
    {
        // Arrange
        var (client, refreshToken) = await CreateUserClientAsync("change3@gmail.com");
        await client.PutAsJsonAsync("/api/auth/password", new
        {
            currentPassword = "senha123",
            newPassword = "novaSenha456",
            confirmationPassword = "novaSenha456"
        });

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync("/api/auth/password", new
        {
            currentPassword = "senha123",
            newPassword = "novaSenha456",
            confirmationPassword = "novaSenha456"
        });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
