using Microsoft.AspNetCore.Hosting;

namespace Authdoc.Tests.Integration;

public class StartupConfigurationTests
{
    [Fact]
    public void Startup_WithShortJwtKey_FailsWithClearMessage()
    {
        // Arrange
        using var baseFactory = new AuthdocApiFactory();
        using var factory = baseFactory.WithWebHostBuilder(builder =>
            builder.UseSetting("Jwt:Key", "key-with-less-than-32-bytes"));

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        // Assert
        Assert.Contains("at least 32 bytes", exception.Message);
    }
}
