using BudgetBuddy.Shared.Kernel.Entities;

namespace BudgetBuddy.Module.Auth.Features.Security.GetSecurityEvents;

public record GetSecurityEventsQuery(
    int PageNumber = 1,
    int PageSize = 50,
    SecurityEventSeverity? MinSeverity = null
) : IRequest<List<SecurityEventResponse>>;

public record SecurityEventResponse(
    Guid Id,
    SecurityEventType EventType,
    SecurityEventSeverity Severity,
    string Message,
    string? UserId,
    string? UserIdentifier,
    string? IpAddress,
    string? Endpoint,
    bool IsAlert,
    bool IsReviewed,
    DateTime CreatedAt
);
