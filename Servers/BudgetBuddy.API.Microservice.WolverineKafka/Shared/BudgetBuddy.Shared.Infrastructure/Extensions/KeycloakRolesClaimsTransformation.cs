using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace BudgetBuddy.Shared.Infrastructure.Extensions;

/// <summary>
/// Keycloak puts realm roles in a nested JSON claim:
///   "realm_access": { "roles": ["budgetbuddy-admin", "user"] }
/// ASP.NET Core's RequireRole() checks flat ClaimTypes.Role claims.
/// This transformation unpacks the nested array into individual role claims.
/// </summary>
internal sealed class KeycloakRolesClaimsTransformation : IClaimsTransformation
{
    private const string RealmAccessClaim = "realm_access";
    private const string RolesKey         = "roles";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var realmAccess = principal.FindFirstValue(RealmAccessClaim);
        if (realmAccess is null)
        {
            return Task.FromResult(principal);
        }

        JsonElement root;
        try { root = JsonDocument.Parse(realmAccess).RootElement; }
        catch (JsonException) { return Task.FromResult(principal); }

        if (!root.TryGetProperty(RolesKey, out var rolesElement) ||
            rolesElement.ValueKind != JsonValueKind.Array)
        {
            return Task.FromResult(principal);
        }

        var identity = (ClaimsIdentity)principal.Identity!;
        foreach (var role in rolesElement.EnumerateArray())
        {
            var roleName = role.GetString();
            if (roleName is null)
            {
                continue;
            }

            if (!identity.HasClaim(ClaimTypes.Role, roleName))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
            }
        }

        return Task.FromResult(principal);
    }
}
