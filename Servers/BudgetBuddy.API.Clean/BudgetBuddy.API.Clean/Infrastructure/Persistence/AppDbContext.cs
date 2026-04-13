using BudgetBuddy.Infrastructure.Persistence.Converters;
using BudgetBuddy.Infrastructure.Security;
using BudgetBuddy.Infrastructure.Security.Encryption;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<User>, IUnitOfWork
{
    private readonly IEncryptionService _encryptionService;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IEncryptionService encryptionService) : base(options)
    {
        _encryptionService = encryptionService;
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryType> CategoryTypes => Set<CategoryType>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Investment> Investments => Set<Investment>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();
    public DbSet<ExchangeRateSnapshot> ExchangeRateSnapshots => Set<ExchangeRateSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ConfigureEncryptedProperties(modelBuilder);
    }

    private void ConfigureEncryptedProperties(ModelBuilder modelBuilder)
    {
        // Transaction.Payee - encrypt payee names for privacy
        modelBuilder.Entity<Transaction>()
            .Property(t => t.Payee)
            .HasConversion(new EncryptedStringConverter(_encryptionService, "TransactionPayee"));

        // Transaction.Note - encrypt personal notes
        modelBuilder.Entity<Transaction>()
            .Property(t => t.Note)
            .HasConversion(new EncryptedStringConverter(_encryptionService, "TransactionNote"));

        // Future: Add more encrypted properties here
        // Examples:
        // - Bank account numbers
        // - API keys
        // - SSN or other PII
    }

    // NodaTime types are handled by Npgsql.EntityFrameworkCore.PostgreSQL.NodaTime plugin
    // No custom converters needed - the plugin maps:
    // - Instant -> PostgreSQL timestamptz
    // - LocalDate -> PostgreSQL date
    // - LocalDateTime -> PostgreSQL timestamp
}
