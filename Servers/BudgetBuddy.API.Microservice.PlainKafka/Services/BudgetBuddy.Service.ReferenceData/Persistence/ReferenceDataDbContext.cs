using BudgetBuddy.Service.ReferenceData.Domain;
using BudgetBuddy.Service.ReferenceData.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.Service.ReferenceData.Persistence;

public class ReferenceDataDbContext(DbContextOptions<ReferenceDataDbContext> options) : AppDbContext(options)
{
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryType> CategoryTypes => Set<CategoryType>();
    public DbSet<AccountSnapshot> AccountSnapshots => Set<AccountSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("referencedata");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReferenceDataDbContext).Assembly);
    }
}
