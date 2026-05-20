using BudgetBuddy.Service.ReferenceData.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.Service.ReferenceData.Persistence.Configurations;

public class AccountSnapshotConfiguration : IEntityTypeConfiguration<AccountSnapshot>
{
    public void Configure(EntityTypeBuilder<AccountSnapshot> builder)
    {
        builder.ToTable("account_snapshots", "referencedata");
        builder.HasKey(a => a.AccountId);
        builder.Property(a => a.UserId).HasMaxLength(256).IsRequired();
        builder.Property(a => a.Name).HasMaxLength(256).IsRequired();
        builder.Property(a => a.Currency).HasMaxLength(10).IsRequired();
        builder.HasIndex(a => a.UserId);
    }
}
