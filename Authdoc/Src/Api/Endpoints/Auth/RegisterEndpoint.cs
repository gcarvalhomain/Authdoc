namespace Authdoc.Infrastructure.Endpoints.Auth;

public class RegisterEndpoint
{
    public int? Age { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
}