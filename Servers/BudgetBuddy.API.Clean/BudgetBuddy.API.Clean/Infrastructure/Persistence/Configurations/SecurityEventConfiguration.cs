using BudgetBuddy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.Infrastructure.Persistence.Configurations;

public class SecurityEventConfiguration : IEntityTypeConfiguration<SecurityEvent>
{
    public void Configure(EntityTypeBuilder<SecurityEvent> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventType)
            .IsRequired();

        builder.Property(e => e.Severity)
            .IsRequired();

        builder.Property(e => e.Message)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.UserIdentifier)
            .HasMaxLength(256);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(e => e.UserAgent)
            .HasMaxLength(500);

        builder.Property(e => e.Endpoint)
            .HasMaxLength(200);

        builder.Property(e => e.Metadata)
            .HasColumnType("jsonb"); // PostgreSQL JSON type for flexible metadata

        // Indexes for common queries
        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_SecurityEvents_CreatedAt");

        builder.HasIndex(e => e.EventType)
            .HasDatabaseName("IX_SecurityEvents_EventType");

        builder.HasIndex(e => e.Severity)
            .HasDatabaseName("IX_SecurityEvents_Severity");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_SecurityEvents_UserId");

        builder.HasIndex(e => e.IpAddress)
            .HasDatabaseName("IX_SecurityEvents_IpAddress");

        builder.HasIndex(e => e.IsAlert)
            .HasDatabaseName("IX_SecurityEvents_IsAlert")
            .HasFilter("\"IsAlert\" = true"); // Partial index for alerts only

        // Composite index for dashboard queries (recent events by severity)
        builder.HasIndex(e => new { e.CreatedAt, e.Severity, e.IsReviewed })
            .HasDatabaseName("IX_SecurityEvents_Dashboard")
            .IsDescending(true, false, false); // CreatedAt descending for recent-first
    }
}
