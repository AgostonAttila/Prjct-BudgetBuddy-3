using BudgetBuddy.Module.Transactions.Domain;
using BudgetBuddy.Shared.Infrastructure.Persistence.Converters;
using BudgetBuddy.Shared.Infrastructure.Security.Encryption;
using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.Module.Transactions.Persistence;

public class TransactionsDbContext(
    DbContextOptions<TransactionsDbContext> options,
    IEncryptionService encryptionService) : DbContext(options), IBulkOperationContext
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public bool BulkOperationInProgress { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("transactions");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransactionsDbContext).Assembly);

        // Encrypted columns for privacy-sensitive fields
        modelBuilder.Entity<Transaction>()
            .Property(t => t.Payee)
            .HasConversion(new EncryptedStringConverter(encryptionService, "TransactionPayee"));

        modelBuilder.Entity<Transaction>()
            .Property(t => t.Note)
            .HasConversion(new EncryptedStringConverter(encryptionService, "TransactionNote"));
    }
}
