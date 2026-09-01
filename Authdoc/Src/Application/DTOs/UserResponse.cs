namespace Authdoc.Application.DTOs;

public class UserResponse
{
    public int Age { get; set; }
    public int  Id { get; set; }
    public string Email { get; set; }= string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Name { get; set; }= string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
}