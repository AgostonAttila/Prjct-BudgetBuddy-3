using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using System.Data.Common;
using System.Security;
using System.Security.Claims;

namespace BudgetBuddy.Shared.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Sets PostgreSQL session variables for Row-Level Security (RLS) policies
/// Ensures database-level authorization by filtering rows based on authenticated user
/// </summary>
public class RowLevelSecurityInterceptor(
    IHttpContextAccessor httpContextAccessor,
    ILogger<RowLevelSecurityInterceptor> logger)
    : DbConnectionInterceptor
{
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (connection is NpgsqlConnection npgsqlConnection)
            await SetRlsVariablesAsync(npgsqlConnection, cancellationToken);

        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private async Task SetRlsVariablesAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;

        try
        {
            using var cmd = connection.CreateCommand();

            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = httpContext.User.IsInRole("Admin");

                if (!string.IsNullOrEmpty(userId))
                {
                    if (!Guid.TryParse(userId, out var parsedUserId))
                        throw new SecurityException($"Invalid userId format in JWT claim: '{userId}'");

                    // Use the parsed GUID's canonical string representation (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)
                    // to guarantee no injection characters are present, even if the raw claim contained unusual formatting.
                    cmd.CommandText = $"SET LOCAL app.current_user_id = '{parsedUserId:D}'; SET LOCAL app.is_admin = '{isAdmin.ToString().ToLower()}';";
                    await cmd.ExecuteNonQueryAsync(cancellationToken);

                    logger.LogDebug("RLS session variables set: user_id={UserId}, is_admin={IsAdmin}", userId, isAdmin);
                }
            }
            else if (httpContext == null)
            {
                // No HTTP context = background job / system operation (Quartz scheduler, startup tasks).
                // Grant admin-level access so system tasks can read across all users.
                cmd.CommandText = "SET LOCAL app.is_admin = 'true';";
                await cmd.ExecuteNonQueryAsync(cancellationToken);

                logger.LogDebug("RLS session variables set for background job context: is_admin=true");
            }
            // Unauthenticated HTTP request: leave variables unset → DB RLS blocks all access (correct).
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set RLS session variables");
            // Don't throw — connection proceeds; first query will fail if RLS is enforced and no policy matches.
        }
    }

    public override void ConnectionOpened(
        DbConnection connection,
        ConnectionEndEventData eventData)
    {
        if (connection is NpgsqlConnection npgsqlConnection)
            SetRlsVariables(npgsqlConnection);

        base.ConnectionOpened(connection, eventData);
    }

    private void SetRlsVariables(NpgsqlConnection connection)
    {
        var httpContext = httpContextAccessor.HttpContext;

        try
        {
            using var cmd = connection.CreateCommand();

            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = httpContext.User.IsInRole("Admin");

                if (!string.IsNullOrEmpty(userId))
                {
                    if (!Guid.TryParse(userId, out var parsedUserId))
                        throw new SecurityException($"Invalid userId format in JWT claim: '{userId}'");

                    cmd.CommandText = $"SET LOCAL app.current_user_id = '{parsedUserId:D}'; SET LOCAL app.is_admin = '{isAdmin.ToString().ToLower()}';";
                    cmd.ExecuteNonQuery();

                    logger.LogDebug("RLS session variables set: user_id={UserId}, is_admin={IsAdmin}", userId, isAdmin);
                }
            }
            else if (httpContext == null)
            {
                // No HTTP context = background job / system operation.
                cmd.CommandText = "SET LOCAL app.is_admin = 'true';";
                cmd.ExecuteNonQuery();

                logger.LogDebug("RLS session variables set for background job context: is_admin=true");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set RLS session variables");
        }
    }
}
