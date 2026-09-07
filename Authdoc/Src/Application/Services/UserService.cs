using Authdoc.Application.DTOs;
using Authdoc.Data;
using Microsoft.EntityFrameworkCore;


namespace Authdoc.Application.Services;

public class UserService
{
    private readonly ManagerDbContext _context;

    public UserService(ManagerDbContext context)
    {
        _context = context;
    }

// Retorna null quando o e-mail ja existe para que o endpoint decida o status HTTP.
    public async Task<UserResponse?> GetByIdAsync(int id)
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

    public async Task<bool> UpdateAsync(int id, UpdateUserRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == id);
        {
            if (user is null)
            {
                return false;
            }

            user.Name = request.Name!;
            user.Email = request.Email;
            user.Age = request.Age;
            user.CreatedAt = DateTime.Now;
        }
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == id);
        {
            if (user is null)
            {
                return false;
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return true;
        }
    }

    public async Task<bool> EmailBelongsToAnotherUserAsync(int id, string email)
    {
        return await _context.Users.AnyAsync(user => user.Email == email && user.Id != id);
    }
}