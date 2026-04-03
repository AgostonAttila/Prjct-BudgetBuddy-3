using System.Text.Json.Serialization;

namespace BudgetBuddy.API.VSA.Common.Domain.Contracts;

public abstract class AuditableEntity
{
    [JsonIgnore]
    public Instant CreatedAt { get; set; }
    [JsonIgnore]
    public Instant? UpdatedAt { get; set; }
}
