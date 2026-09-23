using System;

namespace Authdoc.Models.Entities;

public class User
{
    public int Age { get; set; }
    public Guid Id { get; set; } =  Guid.NewGuid(); 
    public string Email { get; set; }= string.Empty;
    public string Password { get; set; }= string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    

}
public enum UserRole
{
    Admin = 1,
    User = 2
}