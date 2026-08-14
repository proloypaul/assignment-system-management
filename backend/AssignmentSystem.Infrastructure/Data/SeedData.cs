using AssignmentSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AssignmentSystem.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IConfiguration configuration,
        ILogger logger)
    {
        string[] roles = { "Admin", "Teacher", "Student" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                logger.LogInformation("Created role: {Role}", role);
            }
        }

        await SeedUserAsync(userManager, configuration, logger, "Admin", "Admin User");
        await SeedUserAsync(userManager, configuration, logger, "Teacher", "Teacher User");
        await SeedUserAsync(userManager, configuration, logger, "Student", "Student User");
    }

    private static async Task SeedUserAsync(
        UserManager<User> userManager,
        IConfiguration configuration,
        ILogger logger,
        string role,
        string name)
    {
        var email = configuration[$"Seed__{role}Email"];
        var password = configuration[$"Seed__{role}Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Seed credentials for {Role} not found in configuration.", role);
            return;
        }

        if (await userManager.FindByEmailAsync(email) == null)
        {
            var user = new User
            {
                UserName = email,
                Email = email,
                Name = name,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
                logger.LogInformation("Seeded {Role} user successfully.", role);
            }
            else
            {
                logger.LogError("Failed to seed {Role} user. Errors: {Errors}", role, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
