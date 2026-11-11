using System.Net.Http.Headers;
using System.Net.Http.Json;
using Authdoc.Application.DTOs;

namespace Authdoc.Tests.Integration;

public static class ApiTestHelpers
{
    public static async Task<LoginResponse> LoginAsync(this HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
    }

    public static async Task<HttpClient> CreateAdminClientAsync(this AuthdocApiFactory factory)
    {
        var client = factory.CreateClient();
        var login = await client.LoginAsync(AuthdocApiFactory.AdminEmail, AuthdocApiFactory.AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        return client;
    }

    public static async Task<(HttpClient Client, UserResponse User)> CreateUserClientAsync(this AuthdocApiFactory factory, string email)
    {
        const string password = "Test-User-Password-123";

        var adminClient = await factory.CreateAdminClientAsync();
        var user = await adminClient.RegisterUserAsync(email, password);

        var client = factory.CreateClient();
        var login = await client.LoginAsync(email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        return (client, user);
    }

    public static async Task<DocumentResponse> CreateDocumentAsync(this HttpClient client, string name = "My passport")
    {
        var response = await client.PostAsJsonAsync("/api/documents", new
        {
            type = "Passport",
            name,
            number = "FZ123456",
            issuedAt = "2024-01-10",
            expiresAt = "2034-01-10"
        });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<DocumentResponse>())!;
    }

    public static async Task<UserResponse> RegisterUserAsync(this HttpClient adminClient, string email, string password = "senha123")
    {
        var response = await adminClient.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Test User",
            email,
            password,
            confirmationPassword = password
        });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<UserResponse>())!;
    }
}
