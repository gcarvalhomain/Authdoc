namespace Authdoc.Application.DTOs;

public class RegisterUserRequest
{
    public string? Name { get; set; }
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    
}