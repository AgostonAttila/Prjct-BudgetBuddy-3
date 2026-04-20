using System.Text.Json.Serialization;
using NodaTime;

namespace BudgetBuddy.Shared.Kernel.Contracts;

public abstract class AuditableEntity
{
    [JsonIgnore]
    public Instant CreatedAt { get; set; }
    [JsonIgnore]
    public Instant? UpdatedAt { get; set; }
}
