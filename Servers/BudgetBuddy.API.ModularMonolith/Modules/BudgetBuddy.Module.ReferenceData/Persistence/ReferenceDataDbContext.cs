using BudgetBuddy.Module.ReferenceData.Domain;
using BudgetBuddy.Shared.Kernel.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.Module.ReferenceData.Persistence;

public class ReferenceDataDbContext(DbContextOptions<ReferenceDataDbContext> options) : DbContext(options)
{
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryType> CategoryTypes => Set<CategoryType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("referencedata");
        modelBuilder.Entity<User>().ToTable("AspNetUsers", "auth");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReferenceDataDbContext).Assembly);
    }
}
