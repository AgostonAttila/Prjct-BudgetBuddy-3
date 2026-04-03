using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

namespace BudgetBuddy.API.VSA.Common.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Interceptor that automatically creates audit logs for entity changes
/// Tracks Insert, Update, Delete operations with before/after values
/// </summary>
public class  AuditLogInterceptor(
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuditLogInterceptor> logger)
    : SaveChangesInterceptor
{
    // Entities to exclude from auditing
    private readonly HashSet<string> _excludedEntities = new()
    {
        nameof(AuditLog),        // Don't audit the audit log itself!
        nameof(SecurityEvent),   // Already tracked separately
        "IdentityRole",          // ASP.NET Identity tables
        "IdentityUserRole",
        "IdentityUserClaim",
        "IdentityUserLogin",
        "IdentityRoleClaim",
        "IdentityUserToken"
    };

    // Properties to exclude from auditing (sensitive data)
    private readonly HashSet<string> _excludedProperties = new()
    {
        "PasswordHash",
        "SecurityStamp",
        "ConcurrencyStamp",
        "CreatedAt",      // Handled by AuditableEntityInterceptor
        "UpdatedAt",      // Handled by AuditableEntityInterceptor
        "Payee",          // Encrypted column — plaintext must not appear in audit trail
        "Note"            // Encrypted column — plaintext must not appear in audit trail
    };

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context != null)
        {
            await CreateAuditLogs(eventData.Context);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    // Sync SaveChanges removed - entire codebase uses async SaveChangesAsync()
    // Having sync override with .GetAwaiter().GetResult() causes deadlock risk
    // If sync SaveChanges() is accidentally called, base class will handle it safely

    private async Task CreateAuditLogs(DbContext context)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var userId = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userIdentifier = httpContext?.User.FindFirstValue(ClaimTypes.Email)
                           ?? httpContext?.User.FindFirstValue(ClaimTypes.Name);
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified ||
                        e.State == EntityState.Deleted)
            .Where(e => !_excludedEntities.Contains(e.Entity.GetType().Name))
            .ToList();

        foreach (var entry in entries)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    EntityName = entry.Entity.GetType().Name,
                    EntityId = GetPrimaryKeyValue(entry),
                    Operation = GetOperation(entry.State),
                    UserId = userId,
                    UserIdentifier = userIdentifier,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Changes = SerializeChanges(entry)
                };

                await context.Set<AuditLog>().AddAsync(auditLog);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create audit log for entity {EntityType}",
                    entry.Entity.GetType().Name);
                // Don't throw - auditing failures shouldn't break the app
            }
        }
    }

    private static string GetPrimaryKeyValue(EntityEntry entry)
    {
        var keyName = entry.Metadata.FindPrimaryKey()?.Properties.FirstOrDefault()?.Name;

        if (keyName == null)
        {
            return "Unknown";
        }

        var keyValue = entry.Property(keyName).CurrentValue;
        return keyValue?.ToString() ?? "Unknown";
    }

    private static AuditOperation GetOperation(EntityState state)
    {
        return state switch
        {
            EntityState.Added => AuditOperation.Insert,
            EntityState.Modified => AuditOperation.Update,
            EntityState.Deleted => AuditOperation.Delete,
            _ => AuditOperation.Update
        };
    }

    private string? SerializeChanges(EntityEntry entry)
    {
        var changes = new Dictionary<string, object>();

        if (entry.State == EntityState.Added)
        {
            // For INSERT: capture all properties as "NewValue"
            foreach (var property in entry.Properties.Where(p => !_excludedProperties.Contains(p.Metadata.Name)))
            {
                changes[property.Metadata.Name] = new
                {
                    NewValue = property.CurrentValue?.ToString() ?? "null"
                };
            }
        }
        else if (entry.State == EntityState.Modified)
        {
            // For UPDATE: capture changed properties with OldValue and NewValue
            foreach (var property in entry.Properties.Where(p => p.IsModified && !_excludedProperties.Contains(p.Metadata.Name)))
            {
                changes[property.Metadata.Name] = new
                {
                    OldValue = property.OriginalValue?.ToString() ?? "null",
                    NewValue = property.CurrentValue?.ToString() ?? "null"
                };
            }
        }
        else if (entry.State == EntityState.Deleted)
        {
            // For DELETE: capture all properties as "OldValue"
            foreach (var property in entry.Properties.Where(p => !_excludedProperties.Contains(p.Metadata.Name)))
            {
                changes[property.Metadata.Name] = new
                {
                    OldValue = property.OriginalValue?.ToString() ?? "null"
                };
            }
        }

        if (changes.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(changes);
    }
}
