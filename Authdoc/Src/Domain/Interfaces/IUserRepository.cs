namespace Authdoc.Domain.Interfaces;

public class IUserRepository
{
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; }
}