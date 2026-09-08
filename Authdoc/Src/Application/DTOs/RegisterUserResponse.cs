namespace Authdoc.Application.DTOs;

public class RegisterUserResponse
{
    public int Id { get; set; }
    public int Age { get; set; }
    public string Name { get; set; }= string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatdAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}