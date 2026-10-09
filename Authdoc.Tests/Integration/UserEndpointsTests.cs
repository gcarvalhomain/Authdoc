using System.Net;
using System.Net.Http.Json;
using Authdoc.Responses;

namespace Authdoc.Tests.Integration;

public class UserEndpointsTests : IClassFixture<AuthdocApiFactory>
{
    private readonly AuthdocApiFactory _factory;

    public UserEndpointsTests(AuthdocApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetUser_WhenUserDoesNotExist_Returns404UserNotFound()
    {
        // Arrange
        var adminClient = await _factory.CreateAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/users/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("USER_NOT_FOUND", error!.Code);
    }

    [Fact]
    public async Task UpdateUser_WithInvalidEmail_Returns400EmailInvalid()
    {
        // Arrange
        var adminClient = await _factory.CreateAdminClientAsync();
        var user = await adminClient.RegisterUserAsync("update@gmail.com");

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/users/{user.Id}",
            new { name = "Test User", age = 25, email = "abc" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("EMAIL_INVALID", error!.Code);
    }

    [Fact]
    public async Task DeleteUser_WhenDeletingOwnUser_Returns400CannotDeleteOwnUser()
    {
        // Arrange
        var adminClient = await _factory.CreateAdminClientAsync();
        var me = await adminClient.LoginAsync(AuthdocApiFactory.AdminEmail, AuthdocApiFactory.AdminPassword);

        // Act
        var response = await adminClient.DeleteAsync($"/api/users/{me.User.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("CANNOT_DELETE_OWN_USER", error!.Code);
    }

    [Fact]
    public async Task DeleteUser_WhenUserIsNormalUser_Returns204()
    {
        // Arrange
        var adminClient = await _factory.CreateAdminClientAsync();
        var user = await adminClient.RegisterUserAsync("delete@gmail.com");

        // Act
        var response = await adminClient.DeleteAsync($"/api/users/{user.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var getResponse = await adminClient.GetAsync($"/api/users/{user.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task ChangeRole_WithInvalidRole_Returns400RoleInvalid()
    {
        // Arrange
        var adminClient = await _factory.CreateAdminClientAsync();
        var user = await adminClient.RegisterUserAsync("role@gmail.com");

        // Act
        var response = await adminClient.PatchAsJsonAsync($"/api/users/{user.Id}/role", new { role = "SuperAdmin" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ROLE_INVALID", error!.Code);
    }
}
