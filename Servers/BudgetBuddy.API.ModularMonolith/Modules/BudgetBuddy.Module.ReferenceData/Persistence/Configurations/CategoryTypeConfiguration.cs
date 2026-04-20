using BudgetBuddy.Module.ReferenceData.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.Module.ReferenceData.Persistence.Configurations;

public class CategoryTypeConfiguration : IEntityTypeConfiguration<CategoryType>
{
    public void Configure(EntityTypeBuilder<CategoryType> builder)
    {
        builder.HasKey(ct => ct.Id);

        builder.Property(ct => ct.Name)
            .IsRequired()
            .HasMaxLength(200);

        // Internal FK (same module — navigation allowed)
        builder.HasOne(ct => ct.Category)
            .WithMany(c => c.Types)
            .HasForeignKey(ct => ct.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ct => new { ct.CategoryId, ct.Name })
            .IsUnique()
            .HasDatabaseName("IX_CategoryTypes_Unique");
    }
}
