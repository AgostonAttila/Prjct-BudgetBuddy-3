using BudgetBuddy.Module.Investments.Domain;
using BudgetBuddy.Shared.Kernel.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.Module.Investments.Persistence;

public class InvestmentsDbContext(DbContextOptions<InvestmentsDbContext> options) : DbContext(options)
{
    public DbSet<Investment> Investments => Set<Investment>();
    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();
    public DbSet<ExchangeRateSnapshot> ExchangeRateSnapshots => Set<ExchangeRateSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("investments");
        modelBuilder.Entity<User>().ToTable("AspNetUsers", "auth");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvestmentsDbContext).Assembly);
    }
}
