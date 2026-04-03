using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.Services;
using BudgetBuddy.API.VSA.Features.Security.GetSecurityEvents;

namespace BudgetBuddy.API.VSA.Features.Security.GetSecurityAlerts;

public class GetSecurityAlertsHandler(ISecurityEventService securityEventService)
    : IRequestHandler<GetSecurityAlertsQuery, List<SecurityEventResponse>>
{
    public async Task<List<SecurityEventResponse>> Handle(
        GetSecurityAlertsQuery request,
        CancellationToken cancellationToken)
    {
        var alerts = await securityEventService.GetActiveAlertsAsync(cancellationToken);

        return alerts.Select(e => new SecurityEventResponse(
            e.Id,
            e.EventType,
            e.Severity,
            e.Message,
            e.UserId,
            e.UserIdentifier,
            e.IpAddress,
            e.Endpoint,
            e.IsAlert,
            e.IsReviewed,
            e.CreatedAt.ToDateTimeUtc()
        )).ToList();
    }
}
