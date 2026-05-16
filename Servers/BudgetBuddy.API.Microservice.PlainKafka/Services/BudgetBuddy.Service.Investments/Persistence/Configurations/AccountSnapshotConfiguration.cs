using BudgetBuddy.Service.Investments.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.Service.Investments.Persistence.Configurations;

public class AccountSnapshotConfiguration : IEntityTypeConfiguration<AccountSnapshot>
{
    public void Configure(EntityTypeBuilder<AccountSnapshot> builder)
    {
        builder.ToTable("account_snapshots", "investments");
        builder.HasKey(a => a.AccountId);
        builder.Property(a => a.UserId).HasMaxLength(256).IsRequired();
        builder.Property(a => a.Name).HasMaxLength(256).IsRequired();
        builder.Property(a => a.Currency).HasMaxLength(10).IsRequired();
        builder.Property(a => a.Balance).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.HasIndex(a => a.UserId);
    }
}
