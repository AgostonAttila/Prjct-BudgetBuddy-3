using BudgetBuddy.Domain.Entities;
using BudgetBuddy.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetBuddy.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(t => t.Id);

        // PlainToken is not persisted — it's a transient value set by TokenService for the response
        builder.Ignore(t => t.PlainToken);

        // Token column - HASHED for security
        // Plain tokens are hashed using SHA256 before storing
        // This prevents token theft if database is compromised
        builder.Property(t => t.Token)
            .IsRequired()
            .HasMaxLength(512)
            .HasConversion(new HashedStringConverter());

        // Unique index on Token (for fast lookup and prevent duplicates)
        builder.HasIndex(t => t.Token)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_Token");

        // User relationship
        builder.HasOne(t => t.User)
            .WithMany() // User can have multiple refresh tokens
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Delete tokens when user is deleted

        // Composite index on UserId + CreatedAt (for cleanup queries)
        builder.HasIndex(t => new { t.UserId, t.CreatedAt })
            .HasDatabaseName("IX_RefreshTokens_UserId_CreatedAt");

        // Index on ExpiresAt (for cleanup job - delete expired tokens)
        builder.HasIndex(t => t.ExpiresAt)
            .HasDatabaseName("IX_RefreshTokens_ExpiresAt");

        // ReplacedByToken (nullable, for rotation tracking)
        builder.Property(t => t.ReplacedByToken)
            .HasMaxLength(512)
            .IsRequired(false);

        // Revocation fields
        builder.Property(t => t.RevokedAt)
            .IsRequired(false);

        builder.Property(t => t.RevokedReason)
            .HasMaxLength(500)
            .IsRequired(false);

        // IP tracking
        builder.Property(t => t.CreatedByIp)
            .HasMaxLength(45) // IPv6 max length
            .IsRequired(false);

        builder.Property(t => t.RevokedByIp)
            .HasMaxLength(45)
            .IsRequired(false);

        // Timestamps
        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.ExpiresAt)
            .IsRequired();
    }
}
