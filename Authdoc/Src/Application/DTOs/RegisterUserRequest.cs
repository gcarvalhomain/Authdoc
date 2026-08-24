namespace Authdoc.Application.DTOs;

public class RegisterUserRequest
{
    public string? Name { get; set; }
    public int Age { get; set; }
    public string? Email { get; set; }
    public string? PasswordHash { get; set; } = string.Empty;
    public string? ConfirmPassword { get; set; }
    
}