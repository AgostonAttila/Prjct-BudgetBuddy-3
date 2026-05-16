using BudgetBuddy.Service.Budgets.Domain;
using BudgetBuddy.Service.Budgets.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.Service.Budgets.Persistence;

public class BudgetsDbContext(DbContextOptions<BudgetsDbContext> options) : AppDbContext(options)
{
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<CategorySnapshot> CategorySnapshots => Set<CategorySnapshot>();
    public DbSet<CategorySpendingAggregate> CategorySpendingAggregates => Set<CategorySpendingAggregate>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("budgets");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BudgetsDbContext).Assembly);
    }
}
