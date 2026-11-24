using System.Net;
using System.Net.Http.Json;

namespace Authdoc.Tests.Integration;

// Behind Azure App Service, every request comes from Azure's front-end proxy, which adds the
// real client IP to the X-Forwarded-For header. The rate limit must count each client IP alone.
public class ForwardedHeadersTests : IClassFixture<RateLimitedApiFactory>
{
    private readonly RateLimitedApiFactory _factory;

    public ForwardedHeadersTests(RateLimitedApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithForwardedClientIp_CountsEachClientIpAlone()
    {
        // Arrange
        // App Service sends the client IP with its port, like "203.0.113.10:51234".
        var firstClient = CreateClientBehindProxy("203.0.113.10:51234");
        var secondClient = CreateClientBehindProxy("203.0.113.20:40000");

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var allowed = await firstClient.PostAsJsonAsync("/api/auth/login", WrongLogin());
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        // Act
        var blocked = await firstClient.PostAsJsonAsync("/api/auth/login", WrongLogin());
        var otherClient = await secondClient.PostAsJsonAsync("/api/auth/login", WrongLogin());

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, otherClient.StatusCode);
    }

    [Fact]
    public async Task Login_WithFakeForwardedIpBeforeTheRealOne_CountsTheRealIp()
    {
        // Arrange
        // A client sends its own fake X-Forwarded-For. The proxy adds the real IP at the end,
        // and only the last value is read, so changing the fake value does not help.
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var client = CreateClientBehindProxy($"198.51.100.{attempt}, 203.0.113.30:51234");
            var allowed = await client.PostAsJsonAsync("/api/auth/login", WrongLogin());
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        var attacker = CreateClientBehindProxy("198.51.100.99, 203.0.113.30:51234");

        // Act
        var response = await attacker.PostAsJsonAsync("/api/auth/login", WrongLogin());

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    private HttpClient CreateClientBehindProxy(string forwardedFor)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", forwardedFor);
        return client;
    }

    private static object WrongLogin()
    {
        return new { email = "proxy-test@gmail.com", password = "wrong-password" };
    }
}

// In development there is no proxy, so X-Forwarded-For must be ignored: otherwise anyone
// could send a new fake IP on every request and never reach the rate limit.
public class ForwardedHeadersDisabledTests : IClassFixture<RateLimitedWithoutProxyApiFactory>
{
    private readonly RateLimitedWithoutProxyApiFactory _factory;

    public ForwardedHeadersDisabledTests(RateLimitedWithoutProxyApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithForwardedHeadersOff_IgnoresTheForwardedIp()
    {
        // Arrange
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Forwarded-For", $"203.0.113.{attempt}");
            var allowed = await client.PostAsJsonAsync("/api/auth/login", new { email = "proxy-off@gmail.com", password = "wrong-password" });
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        var spoofing = _factory.CreateClient();
        spoofing.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.99");

        // Act
        var response = await spoofing.PostAsJsonAsync("/api/auth/login", new { email = "proxy-off@gmail.com", password = "wrong-password" });

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }
}
