using Authdoc.Models.Entities;

namespace Authdoc.Application.DTOs;

public class UpdateUserRequest
{
    public string? Name { get; set; } = string.Empty; 
    public string Email { get; set; } =  string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
}