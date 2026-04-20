using BudgetBuddy.Module.Budgets.Domain;
using BudgetBuddy.Shared.Kernel.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.Module.Budgets.Persistence;

public class BudgetsDbContext(DbContextOptions<BudgetsDbContext> options) : DbContext(options)
{
    public DbSet<Budget> Budgets => Set<Budget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("budgets");
        modelBuilder.Entity<User>().ToTable("AspNetUsers", "auth");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BudgetsDbContext).Assembly);
    }
}
