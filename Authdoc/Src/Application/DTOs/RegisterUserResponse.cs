namespace Authdoc.Application.DTOs;

public class RegisterUserResponse
{
    public int Id { get; set; }
    public int Age { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatdAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}