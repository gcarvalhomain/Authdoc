using System;
using System.Threading.Tasks;
using Authdoc.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace Authdoc.Data;

public static class DbInitializer
{
    public static async Task SeedAdminAsync(ManagerDbContext context, IPasswordHasher<User> passwordHasher, string? adminPassword)
    {
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }

        bool hasAdmin = await context.Users.AnyAsync(u => u.Role == UserRole.Admin);

        if (hasAdmin) return;

        string adminEmail = "admin@system.com";

        // Anyone can sign up with the admin email, so an existing account is never promoted:
        // the person who created it would get the Admin role without knowing the seed password.
        bool emailIsTaken = await context.Users.AnyAsync(u => u.Email == adminEmail);
        if (emailIsTaken)
        {
            throw new InvalidOperationException(
                $"There is no admin, and the email {adminEmail} already belongs to a normal user. " +
                "Delete or rename that user before starting the API. See README.md for instructions.");
        }

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException("SeedData:AdminPassword is not configured. See README.md for instructions.");
        }

        var now = DateTime.UtcNow;
        var initialAdmin = new User
        {
            Id = Guid.NewGuid(),
            Name = "Start Admin",
            Email = adminEmail,
            Role = UserRole.Admin,
            CreatedAt = now,
            UpdatedAt = now
        };

        initialAdmin.PasswordHash = passwordHasher.HashPassword(initialAdmin, adminPassword);

        await context.Users.AddAsync(initialAdmin);
        await context.SaveChangesAsync();
    }
}