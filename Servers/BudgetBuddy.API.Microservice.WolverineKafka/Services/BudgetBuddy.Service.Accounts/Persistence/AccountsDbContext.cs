using BudgetBuddy.Service.Accounts.Domain;
using BudgetBuddy.Service.Accounts.ReadModels;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace BudgetBuddy.Service.Accounts.Persistence;

public class AccountsDbContext(DbContextOptions<AccountsDbContext> options) : AppDbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountTransactionTotals> AccountTransactionTotals => Set<AccountTransactionTotals>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("accounts");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AccountsDbContext).Assembly);
        modelBuilder.MapWolverineEnvelopeStorage("wolverine");
    }
}
