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

    [Fact]
    public async Task GetDocument_WhenDocumentBelongsToUser_Returns200WithDocument()
    {
        // Arrange
        var (client, _) = await _factory.CreateUserClientAsync("get-own@gmail.com");
        var created = await client.CreateDocumentAsync();

        // Act
        var response = await client.GetAsync($"/api/documents/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadFromJsonAsync<DocumentResponse>();
        Assert.Equal(created.Id, document!.Id);
        Assert.Equal("My passport", document.Name);
    }

    [Fact]
    public async Task GetDocument_WhenDocumentDoesNotExist_Returns404DocumentNotFound()
    {
        // Arrange
        var (client, _) = await _factory.CreateUserClientAsync("get-missing@gmail.com");

        // Act
        var response = await client.GetAsync($"/api/documents/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("DOCUMENT_NOT_FOUND", error!.Code);
    }

    [Fact]
    public async Task GetDocument_WhenDocumentBelongsToAnotherUser_Returns404DocumentNotFound()
    {
        // Arrange
        var (ownerClient, _) = await _factory.CreateUserClientAsync("get-owner@gmail.com");
        var (otherClient, _) = await _factory.CreateUserClientAsync("get-other@gmail.com");
        var created = await ownerClient.CreateDocumentAsync();

        // Act
        var response = await otherClient.GetAsync($"/api/documents/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("DOCUMENT_NOT_FOUND", error!.Code);
    }

    [Fact]
    public async Task GetDocument_WhenRequestedByAdmin_Returns404DocumentNotFound()
    {
        // Arrange
        var (ownerClient, _) = await _factory.CreateUserClientAsync("get-admin-owner@gmail.com");
        var adminClient = await _factory.CreateAdminClientAsync();
        var created = await ownerClient.CreateDocumentAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/documents/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("DOCUMENT_NOT_FOUND", error!.Code);
    }

    [Fact]
    public async Task GetDocument_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/documents/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("UNAUTHORIZED", error!.Code);
    }

    [Fact]
    public async Task ListDocuments_WithDocumentsOfTwoUsers_ReturnsOnlyOwnDocuments()
    {
        // Arrange
        var (client, _) = await _factory.CreateUserClientAsync("list-own@gmail.com");
        var (otherClient, _) = await _factory.CreateUserClientAsync("list-other@gmail.com");
        await client.CreateDocumentAsync("My passport");
        await client.CreateDocumentAsync("My visa");
        await otherClient.CreateDocumentAsync("Other user passport");

        // Act
        var response = await client.GetAsync("/api/documents");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<DocumentResponse>>();
        Assert.Equal(2, page!.TotalCount);
        Assert.Equal(["My passport", "My visa"], page.Items.Select(item => item.Name));
    }

    [Fact]
    public async Task ListDocuments_WithMoreDocumentsThanPageSize_ReturnsRequestedPage()
    {
        // Arrange
        var (client, _) = await _factory.CreateUserClientAsync("list-paged@gmail.com");
        await client.CreateDocumentAsync("First");
        await client.CreateDocumentAsync("Second");
        await client.CreateDocumentAsync("Third");

        // Act
        var response = await client.GetAsync("/api/documents?page=2&pageSize=2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<DocumentResponse>>();
        Assert.Equal(2, page!.Page);
        Assert.Equal(2, page.PageSize);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal("Third", Assert.Single(page.Items).Name);
    }

    [Fact]
    public async Task ListDocuments_WithoutDocuments_ReturnsEmptyPage()
    {
        // Arrange
        var (client, _) = await _factory.CreateUserClientAsync("list-empty@gmail.com");

        // Act
        var response = await client.GetAsync("/api/documents");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<DocumentResponse>>();
        Assert.Equal(0, page!.TotalCount);
        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task ListDocuments_WithPageZero_Returns400PageInvalid()
    {
        // Arrange
        var (client, _) = await _factory.CreateUserClientAsync("list-page-zero@gmail.com");

        // Act
        var response = await client.GetAsync("/api/documents?page=0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("PAGE_INVALID", error!.Code);
    }

    [Fact]
    public async Task ListDocuments_WithPageSizeAboveLimit_Returns400PageSizeInvalid()
    {
        // Arrange
        var (client, _) = await _factory.CreateUserClientAsync("list-page-size@gmail.com");

        // Act
        var response = await client.GetAsync("/api/documents?pageSize=101");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("PAGE_SIZE_INVALID", error!.Code);
    }

    [Fact]
    public async Task ListDocuments_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/documents");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("UNAUTHORIZED", error!.Code);
    }

    [Fact]
    public async Task UpdateDocument_WithValidRequest_Returns200WithUpdatedDocument()
    {
        // Arrange
        var (client, _) = await _factory.CreateUserClientAsync("update-valid@gmail.com");
        var created = await client.CreateDocumentAsync();
        var request = new Dictionary<string, object?>
        {
            ["type"] = "Visa",
            ["name"] = "Work visa",
            ["number"] = "V-0042",
            ["description"] = "Valid for one employer",
            ["issuedAt"] = "2025-03-01"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/documents/{created.Id}", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadFromJsonAsync<DocumentResponse>();
        Assert.Equal(created.Id, document!.Id);
        Assert.Equal("Visa", document.Type);
        Assert.Equal("Work visa", document.Name);
        Assert.Equal("V-0042", document.Number);
        Assert.Equal("Valid for one employer", document.Description);
        Assert.Equal(new DateOnly(2025, 3, 1), document.IssuedAt);
        Assert.Null(document.ExpiresAt);
    }

    [Fact]
    public async Task UpdateDocument_WithValidRequest_SavesTheChanges()
    {
        // Arrange
        var (client, _) = await _factory.CreateUserClientAsync("update-saved@gmail.com");
        var created = await client.CreateDocumentAsync();
        var request = ValidDocument();
        request["name"] = "Renewed passport";

        // Act
        await client.PutAsJsonAsync($"/api/documents/{created.Id}", request);

        // Assert
        var saved = await client.GetFromJsonAsync<DocumentResponse>($"/api/documents/{created.Id}");
        Assert.Equal("Renewed passport", saved!.Name);
    }

    [Fact]
    public async Task UpdateDocument_WithSpacesAroundText_SavesTrimmedValues()
    {
        // Arrange
        var (client, _) = await _factory.CreateUserClientAsync("update-trim@gmail.com");
        var created = await client.CreateDocumentAsync();
        var request = ValidDocument();
        request["name"] = "  Renewed passport  ";
        request["description"] = "   ";

        // Act
        var response = await client.PutAsJsonAsync($"/api/documents/{created.Id}", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadFromJsonAsync<DocumentResponse>();
        Assert.Equal("Renewed passport", document!.Name);
        Assert.Null(document.Description);
    }

    [Theory]
    [MemberData(nameof(InvalidTextFields))]
    public async Task UpdateDocument_WithInvalidTextField_Returns400WithFieldError(string field, string value, string expectedCode)
    {
        // Arrange
        var (client, _) = await _factory.CreateUserClientAsync($"update-{Guid.NewGuid()}@gmail.com");
        var created = await client.CreateDocumentAsync();
        var request = ValidDocument();
        request[field] = value;

        // Act
        var response = await client.PutAsJsonAsync($"/api/documents/{created.Id}", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal(expectedCode, error!.Code);
    }

    [Fact]
    public async Task UpdateDocument_WithoutIssuedAt_Returns400IssuedAtRequired()
    {
        // Arrange
        var (client, _) = await _factory.CreateUserClientAsync("update-no-issued@gmail.com");
        var created = await client.CreateDocumentAsync();
        var request = ValidDocument();
        request.Remove("issuedAt");

        // Act
        var response = await client.PutAsJsonAsync($"/api/documents/{created.Id}", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ISSUED_AT_REQUIRED", error!.Code);
    }

    [Fact]
    public async Task UpdateDocument_WithExpiresAtBeforeIssuedAt_Returns400ExpiresAtBeforeIssuedAt()
    {
        // Arrange
        var (client, _) = await _factory.CreateUserClientAsync("update-expires-before@gmail.com");
        var created = await client.CreateDocumentAsync();
        var request = ValidDocument();
        request["issuedAt"] = "2024-01-10";
        request["expiresAt"] = "2024-01-09";

        // Act
        var response = await client.PutAsJsonAsync($"/api/documents/{created.Id}", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("EXPIRES_AT_BEFORE_ISSUED_AT", error!.Code);
    }

    [Fact]
    public async Task UpdateDocument_WhenDocumentDoesNotExist_Returns404DocumentNotFound()
    {
        // Arrange
        var (client, _) = await _factory.CreateUserClientAsync("update-missing@gmail.com");

        // Act
        var response = await client.PutAsJsonAsync($"/api/documents/{Guid.NewGuid()}", ValidDocument());

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("DOCUMENT_NOT_FOUND", error!.Code);
    }

    [Fact]
    public async Task UpdateDocument_WhenDocumentBelongsToAnotherUser_Returns404DocumentNotFound()
    {
        // Arrange
        var (ownerClient, _) = await _factory.CreateUserClientAsync("update-owner@gmail.com");
        var (otherClient, _) = await _factory.CreateUserClientAsync("update-other@gmail.com");
        var created = await ownerClient.CreateDocumentAsync();
        var request = ValidDocument();
        request["name"] = "Changed by another user";

        // Act
        var response = await otherClient.PutAsJsonAsync($"/api/documents/{created.Id}", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("DOCUMENT_NOT_FOUND", error!.Code);
    }

    [Fact]
    public async Task UpdateDocument_WhenDocumentBelongsToAnotherUser_KeepsTheDocumentUnchanged()
    {
        // Arrange
        var (ownerClient, _) = await _factory.CreateUserClientAsync("update-keep-owner@gmail.com");
        var (otherClient, _) = await _factory.CreateUserClientAsync("update-keep-other@gmail.com");
        var created = await ownerClient.CreateDocumentAsync("My passport");
        var request = ValidDocument();
        request["name"] = "Changed by another user";

        // Act
        await otherClient.PutAsJsonAsync($"/api/documents/{created.Id}", request);

        // Assert
        var saved = await ownerClient.GetFromJsonAsync<DocumentResponse>($"/api/documents/{created.Id}");
        Assert.Equal("My passport", saved!.Name);
    }

    [Fact]
    public async Task UpdateDocument_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync($"/api/documents/{Guid.NewGuid()}", ValidDocument());

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("UNAUTHORIZED", error!.Code);
    }
}
