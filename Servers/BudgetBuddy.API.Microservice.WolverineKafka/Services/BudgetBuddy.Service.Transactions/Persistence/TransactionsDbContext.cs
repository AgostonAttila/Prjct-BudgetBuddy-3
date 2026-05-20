using BudgetBuddy.Service.Transactions.Domain;
using BudgetBuddy.Service.Transactions.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Persistence.Converters;
using BudgetBuddy.Shared.Infrastructure.Security.Encryption;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace BudgetBuddy.Service.Transactions.Persistence;

public class TransactionsDbContext(
    DbContextOptions<TransactionsDbContext> options,
    IEncryptionService encryptionService) : AppDbContext(options), IBulkOperationContext
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<AccountSnapshot> AccountSnapshots => Set<AccountSnapshot>();
    public DbSet<CategorySnapshot> CategorySnapshots => Set<CategorySnapshot>();
    public bool BulkOperationInProgress { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("transactions");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransactionsDbContext).Assembly);
        modelBuilder.MapWolverineEnvelopeStorage("wolverine");

        // Encrypted columns for privacy-sensitive fields
        modelBuilder.Entity<Transaction>()
            .Property(t => t.Payee)
            .HasConversion(new EncryptedStringConverter(encryptionService, "TransactionPayee"));

        modelBuilder.Entity<Transaction>()
            .Property(t => t.Note)
            .HasConversion(new EncryptedStringConverter(encryptionService, "TransactionNote"));
    }
}
