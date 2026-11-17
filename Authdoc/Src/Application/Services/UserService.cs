using System;
using System.Threading.Tasks;
using Authdoc.Application.DTOs;
using Authdoc.Data;
using Authdoc.Models.Entities;
using Microsoft.EntityFrameworkCore;


namespace Authdoc.Application.Services;

public class UserService
{
    private readonly ManagerDbContext _context;

    public UserService(ManagerDbContext context)
    {
        _context = context;
    }

    public async Task<UserResponse?> GetByIdAsync(Guid id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == id);
        if (user is null)
        {
            return null;
        }

        return UserResponse.From(user);
    }

    public async Task<PagedResponse<UserResponse>> ListAsync(int page, int pageSize)
    {
        var totalCount = await _context.Users.CountAsync();

        // (page - 1) * pageSize may not fit in an int, so the skip is calculated as a long.
        var skip = (long)(page - 1) * pageSize;

        var users = new List<User>();

        // A page after the last one has no items, so the query is not needed.
        if (skip < totalCount)
        {
            users = await _context.Users
                .OrderBy(user => user.CreatedAt)
                .ThenBy(user => user.Id)
                .Skip((int)skip)
                .Take(pageSize)
                .ToListAsync();
        }

        return new PagedResponse<UserResponse>
        {
            Items = users.Select(UserResponse.From).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<UpdateUserResult> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == id);

        if (user is null)
        {
            return UpdateUserResult.UserNotFound;
        }

        user.Name = request.Name;
        user.Email = request.Email;
        user.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            return UpdateUserResult.EmailAlreadyInUse;
        }

        return UpdateUserResult.Updated;
    }

    public async Task<ChangeRoleResult> ChangeRoleAsync(Guid id, UserRole role)
    {
        var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == id);

        if (user is null)
        {
            return ChangeRoleResult.UserNotFound;
        }

        if (user.Role == role)
        {
            return ChangeRoleResult.Success;
        }

        if (user.Role == UserRole.Admin)
        {
            var adminCount = await _context.Users.CountAsync(u => u.Role == UserRole.Admin);
            if (adminCount <= 1)
            {
                return ChangeRoleResult.LastAdmin;
            }
        }

        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return ChangeRoleResult.Success;
    }

    public async Task<DeleteUserResult> DeleteAsync(Guid id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == id);

        if (user is null)
        {
            return DeleteUserResult.UserNotFound;
        }

        if (user.Role == UserRole.Admin)
        {
            var adminCount = await _context.Users.CountAsync(u => u.Role == UserRole.Admin);
            if (adminCount <= 1)
            {
                return DeleteUserResult.LastAdmin;
            }
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return DeleteUserResult.Deleted;
    }

    // A token is accepted only while its user still exists and still has the role written in it.
    // So a deleted user or a changed role makes the old token stop working at once.
    public async Task<bool> IsTokenStillValidAsync(Guid id, string? tokenRole)
    {
        var currentRole = await _context.Users
            .Where(user => user.Id == id)
            .Select(user => (UserRole?)user.Role)
            .FirstOrDefaultAsync();

        return currentRole is not null && currentRole.Value.ToString() == tokenRole;
    }

    public async Task<bool> EmailBelongsToAnotherUserAsync(Guid id, string email)
    {

        return await _context.Users.AnyAsync(user => user.Email == email && user.Id != id);
    }
}

public enum UpdateUserResult
{
    Updated,
    UserNotFound,
    EmailAlreadyInUse
}

public enum ChangeRoleResult
{
    Success,
    UserNotFound,
    LastAdmin
}

public enum DeleteUserResult
{
    Deleted,
    UserNotFound,
    LastAdmin
}