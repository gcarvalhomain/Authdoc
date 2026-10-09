using System.Net;
using System.Net.Http.Json;
using Authdoc.Responses;

namespace Authdoc.Tests.Integration;

public class RateLimitTests : IClassFixture<RateLimitedApiFactory>
{
    private readonly RateLimitedApiFactory _factory;

    public RateLimitTests(RateLimitedApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_AfterFiveAttempts_Returns429TooManyRequests()
    {
        // Arrange
        var client = _factory.CreateClient();
        var wrongLogin = new { email = AuthdocApiFactory.AdminEmail, password = "wrong-password" };

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var allowed = await client.PostAsJsonAsync("/api/auth/login", wrongLogin);
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", wrongLogin);

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("TOO_MANY_REQUESTS", error!.Code);
    }
}
