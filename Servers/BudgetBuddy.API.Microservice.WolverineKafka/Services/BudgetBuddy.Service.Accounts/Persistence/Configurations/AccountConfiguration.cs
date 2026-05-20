using BudgetBuddy.Service.Accounts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.Service.Accounts.Persistence.Configurations;

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

        builder.Property(a => a.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.HasIndex(a => new { a.UserId, a.Name })
            .IsUnique()
            .HasDatabaseName("IX_Accounts_Unique");
    }
}
