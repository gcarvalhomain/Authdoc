namespace Authdoc.Models.Entities;

public class User
{
    public int Age { get; set; }
    public int  Id { get; set; }
    public string Email { get; set; }= string.Empty;
    public string PasswordHash { get; set; }= string.Empty;
    public string Gender { get; set; }
    public string Name { get; set; }= string.Empty;
    public DateTime CreatdAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Role { get; set; } = "User";
    
}