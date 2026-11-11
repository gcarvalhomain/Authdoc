using System.Net;
using System.Net.Http.Json;
using Authdoc.Application.DTOs;
using Authdoc.Data;
using Authdoc.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Authdoc.Tests.Integration;

public class DocumentEndpointsTests : IClassFixture<AuthdocApiFactory>
{
    private readonly AuthdocApiFactory _factory;

    public DocumentEndpointsTests(AuthdocApiFactory factory)
    {
        _factory = factory;
    }

    private static Dictionary<string, object?> ValidDocument()
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "Passport",
            ["name"] = "My passport",
            ["number"] = "FZ123456",
            ["description"] = "Brazilian passport",
            ["issuedAt"] = "2024-01-10",
            ["expiresAt"] = "2034-01-10"
        };
    }

    public static TheoryData<string, string, string> InvalidTextFields => new()
    {
        { "type", "", "TYPE_REQUIRED" },
        { "type", "   ", "TYPE_REQUIRED" },
        { "type", new string('a', 51), "TYPE_TOO_LONG" },
        { "name", "", "NAME_REQUIRED" },
        { "name", new string('a', 101), "NAME_TOO_LONG" },
        { "number", "", "NUMBER_REQUIRED" },
        { "number", new string('a', 51), "NUMBER_TOO_LONG" },
        { "description", new string('a', 501), "DESCRIPTION_TOO_LONG" }
    };

    [Fact]
    public async Task CreateDocument_WithValidRequest_Returns201WithDocument()
    {
        // Arrange
        var (client, _) = await _factory.CreateUserClientAsync("create-valid@gmail.com");

        // Act
        var response = await client.PostAsJsonAsync("/api/documents", ValidDocument());

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var document = await response.Content.ReadFromJsonAsync<DocumentResponse>();
        Assert.Equal($"/api/documents/{document!.Id}", response.Headers.Location?.ToString());
        Assert.Equal("Passport", document.Type);
        Assert.Equal("My passport", document.Name);
        Assert.Equal("FZ123456", document.Number);
        Assert.Equal("Brazilian passport", document.Description);
        Assert.Equal(new DateOnly(2024, 1, 10), document.IssuedAt);
        Assert.Equal(new DateOnly(2034, 1, 10), document.ExpiresAt);
    }

    [Fact]
    public async Task CreateDocument_WithOwnerIdInBody_SavesDocumentForTokenUser()
    {
        // Arrange
        var (client, user) = await _factory.CreateUserClientAsync("create-owner@gmail.com");
        var request = ValidDocument();
        request["ownerId"] = Guid.NewGuid();

        // Act
        var response = await client.PostAsJsonAsync("/api/documents", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var document = await response.Content.ReadFromJsonAsync<DocumentResponse>();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ManagerDbContext>();
        var saved = await context.Documents.SingleAsync(d => d.Id == document!.Id);
        Assert.Equal(user.Id, saved.OwnerId);
    }

    [Fact]
    public async Task CreateDocument_WithSpacesAroundText_SavesTrimmedValues()
    {
        // Arrange
        var (client, _) = await _factory.CreateUserClientAsync("create-trim@gmail.com");
        var request = ValidDocument();
        request["type"] = "  Passport  ";
        request["name"] = "  My passport  ";
        request["number"] = "  FZ123456  ";
        request["description"] = "  Brazilian passport  ";

        // Act
        var response = await client.PostAsJsonAsync("/api/documents", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var document = await response.Content.ReadFromJsonAsync<DocumentResponse>();
        Assert.Equal("Passport", document!.Type);
        Assert.Equal("My passport", document.Name);
        Assert.Equal("FZ123456", document.Number);
        Assert.Equal("Brazilian passport", document.Description);
    }

    [Fact]
    public async Task CreateDocument_WithoutDescriptionAndExpiresAt_Returns201WithEmptyOptionalFields()
    {
        // Arrange
        var (client, _) = await _factory.CreateUserClientAsync("create-optional@gmail.com");
        var request = ValidDocument();
        request.Remove("description");
        request.Remove("expiresAt");

        // Act
        var response = await client.PostAsJsonAsync("/api/documents", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var document = await response.Content.ReadFromJsonAsync<DocumentResponse>();
        Assert.Null(document!.Description);
        Assert.Null(document.ExpiresAt);
    }

    [Fact]
    public async Task CreateDocument_WithBlankDescription_SavesNoDescription()
    {
        // Arrange
        var (client, _) = await _factory.CreateUserClientAsync("create-blank-description@gmail.com");
        var request = ValidDocument();
        request["description"] = "   ";

        // Act
        var response = await client.PostAsJsonAsync("/api/documents", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var document = await response.Content.ReadFromJsonAsync<DocumentResponse>();
        Assert.Null(document!.Description);
    }

    [Fact]
    public async Task CreateDocument_WithAlreadyExpiredDocument_Returns201()
    {
        // Arrange
        var (client, _) = await _factory.CreateUserClientAsync("create-expired@gmail.com");
        var request = ValidDocument();
        request["issuedAt"] = "2010-05-01";
        request["expiresAt"] = "2020-05-01";

        // Act
        var response = await client.PostAsJsonAsync("/api/documents", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(InvalidTextFields))]
    public async Task CreateDocument_WithInvalidTextField_Returns400WithFieldError(string field, string value, string expectedCode)
    {
        // Arrange
        var (client, _) = await _factory.CreateUserClientAsync($"create-{Guid.NewGuid()}@gmail.com");
        var request = ValidDocument();
        request[field] = value;

        // Act
        var response = await client.PostAsJsonAsync("/api/documents", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal(expectedCode, error!.Code);
    }

    [Fact]
    public async Task CreateDocument_WithoutIssuedAt_Returns400IssuedAtRequired()
    {
        // Arrange
        var (client, _) = await _factory.CreateUserClientAsync("create-no-issued@gmail.com");
        var request = ValidDocument();
        request.Remove("issuedAt");

        // Act
        var response = await client.PostAsJsonAsync("/api/documents", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ISSUED_AT_REQUIRED", error!.Code);
    }

    [Fact]
    public async Task CreateDocument_WithIssuedAtInFuture_Returns400IssuedAtInFuture()
    {
        // Arrange
        var (client, _) = await _factory.CreateUserClientAsync("create-future@gmail.com");
        var request = ValidDocument();
        request["issuedAt"] = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2).ToString("yyyy-MM-dd");
        request.Remove("expiresAt");

        // Act
        var response = await client.PostAsJsonAsync("/api/documents", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ISSUED_AT_IN_FUTURE", error!.Code);
    }

    [Fact]
    public async Task CreateDocument_WithExpiresAtBeforeIssuedAt_Returns400ExpiresAtBeforeIssuedAt()
    {
        // Arrange
        var (client, _) = await _factory.CreateUserClientAsync("create-expires-before@gmail.com");
        var request = ValidDocument();
        request["issuedAt"] = "2024-01-10";
        request["expiresAt"] = "2024-01-09";

        // Act
        var response = await client.PostAsJsonAsync("/api/documents", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("EXPIRES_AT_BEFORE_ISSUED_AT", error!.Code);
    }

    [Fact]
    public async Task CreateDocument_WithExpiresAtEqualToIssuedAt_Returns400ExpiresAtBeforeIssuedAt()
    {
        // Arrange
        var (client, _) = await _factory.CreateUserClientAsync("create-expires-equal@gmail.com");
        var request = ValidDocument();
        request["issuedAt"] = "2024-01-10";
        request["expiresAt"] = "2024-01-10";

        // Act
        var response = await client.PostAsJsonAsync("/api/documents", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("EXPIRES_AT_BEFORE_ISSUED_AT", error!.Code);
    }

    [Fact]
    public async Task CreateDocument_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/documents", ValidDocument());

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("UNAUTHORIZED", error!.Code);
    }
}
