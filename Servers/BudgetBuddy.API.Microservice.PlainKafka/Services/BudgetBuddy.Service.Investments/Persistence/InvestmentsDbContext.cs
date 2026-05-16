using BudgetBuddy.Service.Investments.Domain;
using BudgetBuddy.Service.Investments.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.Service.Investments.Persistence;

public class InvestmentsDbContext(DbContextOptions<InvestmentsDbContext> options) : AppDbContext(options)
{
    public DbSet<Investment> Investments => Set<Investment>();
    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();
    public DbSet<ExchangeRateSnapshot> ExchangeRateSnapshots => Set<ExchangeRateSnapshot>();
    public DbSet<AccountSnapshot> AccountSnapshots => Set<AccountSnapshot>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("investments");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvestmentsDbContext).Assembly);
    }
}
