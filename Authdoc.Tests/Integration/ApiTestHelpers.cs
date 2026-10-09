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

    public static async Task<UserResponse> RegisterUserAsync(this HttpClient adminClient, string email, string password = "senha123")
    {
        var response = await adminClient.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Test User",
            age = 25,
            gender = "Female",
            email,
            password,
            confirmationPassword = password
        });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<UserResponse>())!;
    }
}
