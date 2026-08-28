namespace Authdoc.Infrastructure.Endpoints.Auth;

public class LoginRequest
{
    public string Email  { get; set; } = string.Empty;
    public string? Password { get; set; }
}