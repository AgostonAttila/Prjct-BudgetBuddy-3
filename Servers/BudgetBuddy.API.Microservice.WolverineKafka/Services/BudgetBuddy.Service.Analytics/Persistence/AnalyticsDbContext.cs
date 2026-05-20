using BudgetBuddy.Service.Analytics.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.Service.Analytics.Persistence;

public class AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : AppDbContext(options)
{
    public DbSet<TransactionReadModel> Transactions    { get; set; } = null!;
    public DbSet<BudgetReadModel>      Budgets         { get; set; } = null!;
    public DbSet<InvestmentReadModel>  Investments     { get; set; } = null!;
    public DbSet<CategorySnapshot>     CategorySnapshots { get; set; } = null!;
    public DbSet<AccountSnapshot>        AccountSnapshots  { get; set; } = null!;
    public DbSet<PriceSnapshotReadModel> PriceSnapshots    { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("analytics");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AnalyticsDbContext).Assembly);
    }
}
