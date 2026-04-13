using BudgetBuddy.Domain.Constants;
using Microsoft.AspNetCore.Identity;

namespace BudgetBuddy.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds default admin user
/// SECURITY: Only runs in Development or when explicitly requested with --seed-admin flag
/// </summary>
public class AdminUserSeeder(
    UserManager<User> userManager,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    ILogger<AdminUserSeeder> logger)
    : ISeeder
{
    public async Task Seed()
    {
        // create admin in Development or with explicit flag
        var seedAdminFlag = configuration.GetValue<bool>("SeedAdmin", false);

        if (!environment.IsDevelopment() && !seedAdminFlag)
        {
            logger.LogInformation("Skipping admin user seeding (not in Development and no --seed-admin flag)");
            return;
        }

        logger.LogWarning("Seeding admin user (DEVELOPMENT ONLY - DO NOT USE IN PRODUCTION!)");

        var adminEmail = configuration["AdminUser:Email"] ?? "admin@budgetbuddy.com";
        var adminPassword = configuration["AdminUser:Password"] ?? "Admin@123456"; // TEMPORARY - change immediately!

        // Check if admin already exists
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin != null)
        {
            logger.LogInformation("Admin user already exists: {Email}", adminEmail);

            // Ensure admin has Admin role
            if (!await userManager.IsInRoleAsync(existingAdmin, AppRoles.Admin))
            {
                await userManager.AddToRoleAsync(existingAdmin, AppRoles.Admin);
                logger.LogInformation("Added Admin role to existing user {Email}", adminEmail);
            }

            return;
        }

        // Create admin user
        var adminUser = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true, // Auto-confirm for admin
            TwoFactorEnabled = false, // Can be enabled later
            DefaultCurrency = "USD",
            PreferredLanguage = "en-US",
            FirstName = "Admin",
            LastName = "User"
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);

        if (result.Succeeded)
        {
            // Add to Admin role
            await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);

            logger.LogWarning(
                "Admin user created: {Email} with password: {Password} - CHANGE IMMEDIATELY!",
                adminEmail, adminPassword);
        }
        else
        {
            logger.LogError("Failed to create admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
