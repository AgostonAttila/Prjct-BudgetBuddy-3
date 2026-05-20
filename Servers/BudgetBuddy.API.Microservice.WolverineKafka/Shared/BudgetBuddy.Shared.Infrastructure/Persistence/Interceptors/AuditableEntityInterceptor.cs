using System.Security.Claims;
using BudgetBuddy.Shared.Kernel.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NodaTime;

namespace BudgetBuddy.Shared.Infrastructure.Persistence;

public class AuditableEntityInterceptor(
    IClock clock,
    IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context == null)
        {
            return;
        }

        var now = clock.GetCurrentInstant();
        // Null for background jobs (no HTTP context) — that's intentional.
        var userId = httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                if (userId is not null)
                {
                    entry.Entity.CreatedBy = userId;
                }
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                if (userId is not null)
                {
                    entry.Entity.ModifiedBy = userId;
                }
            }
        }
    }
}
