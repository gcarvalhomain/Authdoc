using Authdoc.Application.DTOs;
using Authdoc.Data;
using Microsoft.EntityFrameworkCore;
using Authdoc.Models.Entities;


namespace Authdoc.Application.Services;

public class UserService
{
    private readonly ManagerDbContext _context;

    public UserService(ManagerDbContext context)
    {
        _context = context;
    }

// Retorna null quando o e-mail ja existe para que o endpoint decida o status HTTP.
    public async Task<UserResponse?> GetByIdAsync (int id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == id);
        if (user is null)
        {
            return null;
        }

        return new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Gender = user.Gender,
            Age = user.Age,
        };
    }

    public async Task<UserResponse> CreateAsync (RegisterUserRequest request)
    {
        var user = new User
        {
            Name = request.Name!,
            Email = request.Email!,
            Age = request.Age
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new UserResponse()
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Gender = user.Gender,
            Age = user.Age,
        };
    }
}