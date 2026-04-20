using BudgetBuddy.Shared.Kernel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.Shared.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntityName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.EntityId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Operation)
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasMaxLength(450); // ASP.NET Identity default

        builder.Property(e => e.UserIdentifier)
            .HasMaxLength(256);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(e => e.UserAgent)
            .HasMaxLength(500);

        builder.Property(e => e.Changes)
            .HasColumnType("jsonb"); // PostgreSQL JSON type

        // Indexes for common audit queries
        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_AuditLogs_CreatedAt");

        builder.HasIndex(e => new { e.EntityName, e.EntityId })
            .HasDatabaseName("IX_AuditLogs_Entity");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_AuditLogs_UserId");

        builder.HasIndex(e => e.Operation)
            .HasDatabaseName("IX_AuditLogs_Operation");

        // Composite index for entity history queries
        builder.HasIndex(e => new { e.EntityName, e.EntityId, e.CreatedAt })
            .HasDatabaseName("IX_AuditLogs_EntityHistory")
            .IsDescending(false, false, true); // CreatedAt descending for recent-first
    }
}
