using Authdoc.Application.DTOs;
using Authdoc.Data;
using Authdoc.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Authdoc.Application.Services;

public class AuthService
{
    private readonly ManagerDbContext _context;
    private readonly PasswordHasher<User>  _passwordHasher;
    private readonly IConfiguration  _configuration;

    public AuthService(ManagerDbContext context, PasswordHasher<User> passwordHasher,  IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public async Task<UserResponse?> RegisterAsync(RegisterUserRequest  request)
    {
        var emailExists = await _context.Users.AnyAsync(user => user.Email == request.Email );
        if (emailExists)
        {
            return null;
        }

        var user = new User
        {
            Name = request.Name!,
            Email = request.Email,
            Age = request.Age,
            CreatdAt = DateTime.UtcNow,
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Age = user.Age,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = user.UpdatedAt
        };
    }
}