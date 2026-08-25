using Authdoc.Data;
using Authdoc.Entities.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace Authdoc.Application.Services;

public class ServiceUser
{
    private readonly ManagerDbContext _context;

    public ServiceUser(ManagerDbContext context, UserManager<User> passwordHasher)
    {
        _context = context;
    }

// Retorna null quando o e-mail ja existe para que o endpoint decida o status HTTP.
    public async Task RegisterUserAsync(User request)
    {
        var userExist = await _context.Users.AnyAsync(user => user.Email == user.Email);
        if (userExist)
        {

        }
    }
}