namespace Authdoc.Application.DTOs;

public class RegisterUserRequest
{
    public required string Name { get; set; }
    public int Age { get; init; }
    public required string Gender { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string ConfirmationPassword { get; set; }
}