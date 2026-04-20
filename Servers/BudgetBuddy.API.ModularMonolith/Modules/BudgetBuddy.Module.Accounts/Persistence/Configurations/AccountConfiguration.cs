using BudgetBuddy.Module.Accounts.Domain;
using BudgetBuddy.Shared.Kernel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.Module.Accounts.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.DefaultCurrencyCode)
            .IsRequired()
            .HasMaxLength(3);

        // FK to User (lives in public schema via AppDbContext)
        // WithMany() — User.Accounts navigation was removed from User entity
        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.UserId, a.Name })
            .IsUnique()
            .HasDatabaseName("IX_Accounts_Unique");
    }
}
