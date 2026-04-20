using BudgetBuddy.Shared.Kernel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.Module.Transactions.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.EventType)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(m => m.Payload)
            .IsRequired();

        builder.Property(m => m.Error)
            .HasMaxLength(2000);

        builder.HasIndex(m => m.ProcessedAt)
            .HasDatabaseName("IX_OutboxMessages_ProcessedAt");

        builder.HasIndex(m => new { m.ProcessedAt, m.RetryCount })
            .HasDatabaseName("IX_OutboxMessages_Processor_Optimized");
    }
}
