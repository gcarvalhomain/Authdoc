using System;
using System.Threading.Tasks;
using Authdoc.Models.Entities;
using Microsoft.EntityFrameworkCore;


namespace Authdoc.Data;

public static class DbInitializer
{
    public static async Task SeedAdminAsync(ManagerDbContext context)
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
            string defaultPassword = "AdminPassword123!";
            
            var initialAdmin = new User
            {
                Id = Guid.NewGuid(),
                Name = "Start Admin",
                Email = adminEmail,
                Password = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
                Role = UserRole.Admin
            };

            await context.Users.AddAsync(initialAdmin);
        }
        
        await context.SaveChangesAsync();
        Console.WriteLine("Initialized Admin");
    }
}