using BudgetBuddy.Application.Common.Contracts;

namespace BudgetBuddy.Application.Features.Security.GetSecurityEvents;

public class GetSecurityEventsHandler(ISecurityEventService securityEventService)
    : IRequestHandler<GetSecurityEventsQuery, List<SecurityEventResponse>>
{
    public async Task<List<SecurityEventResponse>> Handle(
        GetSecurityEventsQuery request,
        CancellationToken cancellationToken)
    {
        var events = await securityEventService.GetRecentEventsAsync(
            request.PageSize,
            request.MinSeverity,
            cancellationToken);

        return events.Select(e => new SecurityEventResponse(
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
