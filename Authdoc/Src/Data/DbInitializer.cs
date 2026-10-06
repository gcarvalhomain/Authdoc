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
        await context.Database.MigrateAsync();
        
        bool hasAdmin = await context.Users.AnyAsync(u => u.Role == UserRole.Admin);
        
        if (hasAdmin) return;
        
        string adminEmail = "admin@system.com";
        var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);

        if (existingUser != null)
        {
            existingUser.Role = UserRole.Admin;
        }
        else
        {
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
                Age = 25,
                Gender = "Male",
                Role = UserRole.Admin,
                CreatedAt = now,
                UpdatedAt = now
            };

            initialAdmin.Password = passwordHasher.HashPassword(initialAdmin, adminPassword);

            await context.Users.AddAsync(initialAdmin);
        }
        
        await context.SaveChangesAsync();
        Console.WriteLine("Initialized Admin");
    }
}