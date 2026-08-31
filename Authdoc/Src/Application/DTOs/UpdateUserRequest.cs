namespace Authdoc.Application.DTOs;

public class UpdateUserRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public int Age { get; set; }
}