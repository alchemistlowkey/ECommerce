using System;
using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Repository;

namespace ECommerce;

public static class SeedData
{
    public static async Task EnsureAdminAsync(IServiceProvider services)
    {
        // Apply any pending migrations automatically
        var context = services.GetRequiredService<RepositoryContext>();
        await context.Database.MigrateAsync();

        var userManager = services.GetRequiredService<UserManager<User>>();

        const string adminEmail = "admin@ecommerce.com";
        const string adminPassword = "Admin@123!";

        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin is null)
        {
            admin = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                Role = "Admin",
                CreatedAt = DateTime.Now
            };

            // UserManager hashes the password, validates complexity rules,
            // sets SecurityStamp and ConcurrencyStamp automatically
            var result = await userManager.CreateAsync(admin, adminPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Failed to seed admin user: {errors}");
            }
        }
    }
}
