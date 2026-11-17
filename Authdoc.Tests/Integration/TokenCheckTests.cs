using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Authdoc.Responses;

namespace Authdoc.Tests.Integration;

public class TokenCheckTests : IClassFixture<AuthdocApiFactory>
{
    private const string Password = "Token-Check-Password-123";

    private readonly AuthdocApiFactory _factory;

    public TokenCheckTests(AuthdocApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> LoginClientAsync(string email)
    {
        var client = _factory.CreateClient();
        var login = await client.LoginAsync(email, Password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        return client;
    }

    [Fact]
    public async Task Request_WithTokenOfDeletedUser_Returns401Unauthorized()
    {
        // Arrange
        var adminClient = await _factory.CreateAdminClientAsync();
        var user = await adminClient.RegisterUserAsync("deleted-token@gmail.com", Password);
        var userClient = await LoginClientAsync("deleted-token@gmail.com");
        await adminClient.DeleteAsync($"/api/users/{user.Id}");

        // Act
        var response = await userClient.GetAsync("/api/documents");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("UNAUTHORIZED", error!.Code);
    }

    [Fact]
    public async Task Request_WithAdminTokenAfterDemotion_Returns401Unauthorized()
    {
        // Arrange
        var adminClient = await _factory.CreateAdminClientAsync();
        var user = await adminClient.RegisterUserAsync("demoted-token@gmail.com", Password);
        await adminClient.PatchAsJsonAsync($"/api/users/{user.Id}/role", new { role = "Admin" });
        var demotedClient = await LoginClientAsync("demoted-token@gmail.com");
        await adminClient.PatchAsJsonAsync($"/api/users/{user.Id}/role", new { role = "User" });

        // Act
        var response = await demotedClient.GetAsync("/api/users");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("UNAUTHORIZED", error!.Code);
    }

    [Fact]
    public async Task Request_WithTokenOfUnchangedUser_Returns200()
    {
        // Arrange
        var adminClient = await _factory.CreateAdminClientAsync();
        await adminClient.RegisterUserAsync("unchanged-token@gmail.com", Password);
        var userClient = await LoginClientAsync("unchanged-token@gmail.com");

        // Act
        var response = await userClient.GetAsync("/api/documents");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
