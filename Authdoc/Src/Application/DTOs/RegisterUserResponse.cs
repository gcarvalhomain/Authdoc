namespace Authdoc.Application.DTOs;

public class RegisterUserResponse
{
    public int? Age { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
}