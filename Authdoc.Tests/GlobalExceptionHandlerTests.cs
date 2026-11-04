using System.Text.Json;
using Authdoc.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Authdoc.Tests;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandle_WithUnexpectedException_Returns500WithoutInternalDetails()
    {
        // Arrange
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        var handled = await handler.TryHandleAsync(context, new InvalidOperationException("secret detail"), CancellationToken.None);

        // Assert
        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.DoesNotContain("secret detail", body);

        var error = JsonSerializer.Deserialize<ErrorResponse>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal("INTERNAL_ERROR", error!.Code);
    }
}
