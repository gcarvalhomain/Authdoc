using System;
using System.Threading.Tasks;
using Authdoc.Application.DTOs;
using Authdoc.Data;
using Authdoc.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;


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

    // Returns the saved user with the result, so the endpoint does not read the user again.
    // A second read could find nothing if another request deleted the user in the meantime.
    public async Task<(UpdateUserResult Result, UserResponse? User)> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == id);

        if (user is null)
        {
            return (UpdateUserResult.UserNotFound, null);
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
            return (UpdateUserResult.EmailAlreadyInUse, null);
        }

        return (UpdateUserResult.Updated, UserResponse.From(user));
    }

    // Same idea as UpdateAsync: the user comes back with the result.
    public async Task<(ChangeRoleResult Result, UserResponse? User)> ChangeRoleAsync(Guid id, UserRole role)
    {
        await using var transaction = await BeginTransactionIfRelationalAsync(); // It keeps the admin count and the change together, so two admins cannot remove each other.
        var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == id);

        if (user is null)
        {
            return (ChangeRoleResult.UserNotFound, null);
        }

        if (user.Role == role)
        {
            return (ChangeRoleResult.Success, UserResponse.From(user));
        }

        if (user.Role == UserRole.Admin)
        {
            var adminCount = await CountAdminsWithLockAsync();
            if (adminCount <= 1)
            {
                return (ChangeRoleResult.LastAdmin, null);
            }
        }

        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await CommitIfStartedAsync(transaction); // It frees the locked admin rows only after the change is saved.
        return (ChangeRoleResult.Success, UserResponse.From(user));
    }

    public async Task<DeleteUserResult> DeleteAsync(Guid id)
    {
        await using var transaction = await BeginTransactionIfRelationalAsync(); // Same reason as in ChangeRoleAsync.
        var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == id);

        if (user is null)
        {
            return DeleteUserResult.UserNotFound;
        }

        if (user.Role == UserRole.Admin)
        {
            var adminCount = await CountAdminsWithLockAsync();
            if (adminCount <= 1)
            {
                return DeleteUserResult.LastAdmin;
            }
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        await CommitIfStartedAsync(transaction); // Same reason as in ChangeRoleAsync.

        return DeleteUserResult.Deleted;
    }

    // The InMemory test database has no transactions, so there it runs without one.
    private async Task<IDbContextTransaction?> BeginTransactionIfRelationalAsync()
    {
        return _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync()
            : null;
    }

    private static async Task CommitIfStartedAsync(IDbContextTransaction? transaction)
    {
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }
    }

    // UPDLOCK + HOLDLOCK lock the admin rows until the transaction ends, so a second admin change waits and then sees the new count.
    private async Task<int> CountAdminsWithLockAsync()
    {
        if (!_context.Database.IsRelational())
        {
            return await _context.Users.CountAsync(u => u.Role == UserRole.Admin);
        }

        var adminRole = (int)UserRole.Admin;
        return await _context.Users
            .FromSql($"SELECT * FROM Users WITH (UPDLOCK, HOLDLOCK) WHERE Role = {adminRole}")
            .CountAsync();
    }

    // A token is accepted only while its user still exists and still has the role and the security stamp written in it.
    // So a deleted user, a changed role or a changed password makes the old token stop working at once.
    public async Task<bool> IsTokenStillValidAsync(Guid id, string? tokenRole, string? tokenSecurityStamp)
    {
        var current = await _context.Users
            .Where(user => user.Id == id)
            .Select(user => new { user.Role, user.SecurityStamp })
            .FirstOrDefaultAsync();

        return current is not null
            && current.Role.ToString() == tokenRole
            && current.SecurityStamp.ToString() == tokenSecurityStamp; // It rejects a token made before the last password change.
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