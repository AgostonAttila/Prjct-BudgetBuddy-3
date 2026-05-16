using System.Text.Json.Serialization;
using NodaTime;

namespace BudgetBuddy.Shared.Kernel.Contracts;

public abstract class AuditableEntity
{
    [JsonIgnore]
    public Instant CreatedAt { get; set; }
    [JsonIgnore]
    public Instant? UpdatedAt { get; set; }
    /// <summary>User ID (Keycloak sub claim) that created the entity. Null for background job writes.</summary>
    [JsonIgnore]
    public string? CreatedBy { get; set; }
    /// <summary>User ID (Keycloak sub claim) that last modified the entity. Null for background job writes.</summary>
    [JsonIgnore]
    public string? ModifiedBy { get; set; }
}
