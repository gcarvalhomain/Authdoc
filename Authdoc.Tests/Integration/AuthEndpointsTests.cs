using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Authdoc.Responses;

namespace Authdoc.Tests.Integration;

public class AuthEndpointsTests : IClassFixture<AuthdocApiFactory>
{
    private readonly AuthdocApiFactory _factory;

    public AuthEndpointsTests(AuthdocApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("{\"email\":")]
    [InlineData("{\"email\":\"admin@system.com\"}")]
    public async Task Login_WithUnreadableBody_Returns400InvalidRequest(string body)
    {
        // Arrange
        var client = _factory.CreateClient();
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/auth/login", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("INVALID_REQUEST", error!.Code);
    }

    [Fact]
    public async Task Register_WithUntrimmedUppercaseEmail_StoresNormalizedAndAllowsLogin()
    {
        // Arrange
        var adminClient = await _factory.CreateAdminClientAsync();

        // Act
        var user = await adminClient.RegisterUserAsync("  Normalized@Gmail.com ");
        var login = await _factory.CreateClient().LoginAsync(" NORMALIZED@gmail.com", "senha123");

        // Assert
        Assert.Equal("normalized@gmail.com", user.Email);
        Assert.Equal(user.Id, login.User.Id);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var login = await client.LoginAsync(AuthdocApiFactory.AdminEmail, AuthdocApiFactory.AdminPassword);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(login.Token));
        Assert.Equal("Admin", login.User.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401InvalidCredentials()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = AuthdocApiFactory.AdminEmail, password = "wrong-password" });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("INVALID_CREDENTIALS", error!.Code);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("UNAUTHORIZED", error!.Code);
    }

    [Fact]
    public async Task Me_WithAdminToken_ReturnsRole()
    {
        // Arrange
        var adminClient = await _factory.CreateAdminClientAsync();

        // Act
        var me = await adminClient.GetFromJsonAsync<MeResponse>("/api/auth/me");

        // Assert
        Assert.Equal(AuthdocApiFactory.AdminEmail, me!.Email);
        Assert.Equal("Admin", me.Role);
    }

    private sealed record MeResponse(string Id, string Name, string Email, string Role);

    [Fact]
    public async Task Register_WithExistingEmail_Returns409EmailAlreadyInUse()
    {
        // Arrange
        var adminClient = await _factory.CreateAdminClientAsync();
        await adminClient.RegisterUserAsync("duplicated@gmail.com");

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Other User",
            email = "duplicated@gmail.com",
            password = "senha123",
            confirmationPassword = "senha123"
        });

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("EMAIL_ALREADY_IN_USE", error!.Code);
    }

    [Fact]
    public async Task Register_WithSevenCharacterPassword_Returns400PasswordTooShort()
    {
        // Arrange
        var adminClient = await _factory.CreateAdminClientAsync();

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Short Password",
            email = "shortpassword@gmail.com",
            password = "senha12",
            confirmationPassword = "senha12"
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("PASSWORD_TOO_SHORT", error!.Code);
    }

    [Fact]
    public async Task Register_WithUserToken_Returns403Forbidden()
    {
        // Arrange
        var adminClient = await _factory.CreateAdminClientAsync();
        await adminClient.RegisterUserAsync("forbidden@gmail.com");
        var userClient = _factory.CreateClient();
        var login = await userClient.LoginAsync("forbidden@gmail.com", "senha123");
        userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        // Act
        var response = await userClient.PostAsJsonAsync("/api/auth/register", new { });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("FORBIDDEN", error!.Code);
    }
}
