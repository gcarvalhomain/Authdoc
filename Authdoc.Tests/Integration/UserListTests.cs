using System.Net;
using System.Net.Http.Json;
using Authdoc.Application.DTOs;
using Authdoc.Responses;

namespace Authdoc.Tests.Integration;

public class UserListTests : IClassFixture<AuthdocApiFactory>
{
    private readonly AuthdocApiFactory _factory;

    public UserListTests(AuthdocApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListUsers_WithPageSize_ReturnsRequestedPage()
    {
        // Arrange
        var adminClient = await _factory.CreateAdminClientAsync();
        await adminClient.RegisterUserAsync("list1@gmail.com");
        await adminClient.RegisterUserAsync("list2@gmail.com");
        await adminClient.RegisterUserAsync("list3@gmail.com");

        // Act
        var result = await adminClient.GetFromJsonAsync<PagedResponse<UserResponse>>("/api/users?page=2&pageSize=3");

        // Assert
        Assert.Equal(2, result!.Page);
        Assert.Equal(3, result.PageSize);
        Assert.Equal(4, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Single(result.Items);
    }

    [Theory]
    [InlineData("/api/users?page=0", "PAGE_INVALID")]
    [InlineData("/api/users?pageSize=0", "PAGE_SIZE_INVALID")]
    [InlineData("/api/users?pageSize=101", "PAGE_SIZE_INVALID")]
    public async Task ListUsers_WithInvalidPaging_Returns400(string url, string expectedCode)
    {
        // Arrange
        var adminClient = await _factory.CreateAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal(expectedCode, error!.Code);
    }
}
