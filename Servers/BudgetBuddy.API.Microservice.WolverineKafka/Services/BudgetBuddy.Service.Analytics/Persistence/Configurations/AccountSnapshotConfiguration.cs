using BudgetBuddy.Service.Analytics.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.Service.Analytics.Persistence.Configurations;

public class AccountSnapshotConfiguration : IEntityTypeConfiguration<AccountSnapshot>
{
    public void Configure(EntityTypeBuilder<AccountSnapshot> builder)
    {
        builder.ToTable("account_snapshots", "analytics");
        builder.HasKey(a => a.AccountId);
        builder.Property(a => a.UserId).HasMaxLength(256).IsRequired();
        builder.Property(a => a.Name).HasMaxLength(256).IsRequired();
        builder.Property(a => a.Currency).HasMaxLength(10).IsRequired();
        builder.Property(a => a.Balance).HasPrecision(18, 2);
        builder.HasIndex(a => a.UserId);
    }
}
