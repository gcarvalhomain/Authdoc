using System;

namespace Authdoc.Models.Entities;

public class User
{
    public const int NameMaxLength = 100;
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public Guid SecurityStamp { get; set; } = Guid.NewGuid(); // It changes on a password change, so the access tokens made before it stop working.


}
public enum UserRole
{
    Admin = 1,
    User = 2
}