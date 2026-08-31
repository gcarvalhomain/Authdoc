namespace Authdoc.Application.DTOs;

public class UpdateUserRequest
{
    public string? Name { get; set; } 
    public string Email { get; set; } =  string.Empty;
    public int Age { get; set; }
}