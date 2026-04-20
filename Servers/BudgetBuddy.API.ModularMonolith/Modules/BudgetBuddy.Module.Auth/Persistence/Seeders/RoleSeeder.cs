using BudgetBuddy.Shared.Kernel.Constants;
using Microsoft.AspNetCore.Identity;

namespace BudgetBuddy.Module.Auth.Persistence.Seeders;

/// <summary>
/// Seeds application roles
/// </summary>
public class RoleSeeder : ISeeder
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<RoleSeeder> _logger;

    public RoleSeeder(RoleManager<IdentityRole> roleManager, ILogger<RoleSeeder> logger)
    {
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task Seed()
    {
        _logger.LogInformation("Seeding roles...");

        foreach (var roleName in AppRoles.AllRoles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(roleName));

                if (result.Succeeded)
                {
                    _logger.LogInformation("Role {RoleName} created", roleName);
                }
                else
                {
                    _logger.LogError("Failed to create role {RoleName}: {Errors}",
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                _logger.LogInformation("Role {RoleName} already exists", roleName);
            }
        }

        _logger.LogInformation("Role seeding completed");
    }
}
