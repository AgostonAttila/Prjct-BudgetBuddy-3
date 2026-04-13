namespace BudgetBuddy.Domain.Contracts;

public abstract class AuditableEntity
{
    public Instant CreatedAt { get; set; }
    public Instant? UpdatedAt { get; set; }
}
