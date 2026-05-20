using BudgetBuddy.Service.Budgets.Domain;
using BudgetBuddy.Service.Budgets.ReadModels;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace BudgetBuddy.Service.Budgets.Persistence;

public class BudgetsDbContext(DbContextOptions<BudgetsDbContext> options) : AppDbContext(options)
{
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<CategorySnapshot> CategorySnapshots => Set<CategorySnapshot>();
    public DbSet<CategorySpendingAggregate> CategorySpendingAggregates => Set<CategorySpendingAggregate>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("budgets");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BudgetsDbContext).Assembly);
        modelBuilder.MapWolverineEnvelopeStorage("wolverine");
    }
}
