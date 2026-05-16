using BudgetBuddy.Service.Analytics.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.Service.Analytics.Persistence.Configurations;

public class CategorySnapshotConfiguration : IEntityTypeConfiguration<CategorySnapshot>
{
    public void Configure(EntityTypeBuilder<CategorySnapshot> builder)
    {
        builder.ToTable("category_snapshots", "analytics");
        builder.HasKey(c => c.CategoryId);
        builder.Property(c => c.UserId).HasMaxLength(256).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(256).IsRequired();
        builder.Property(c => c.Icon).HasMaxLength(64);
        builder.HasIndex(c => c.UserId);
    }
}
