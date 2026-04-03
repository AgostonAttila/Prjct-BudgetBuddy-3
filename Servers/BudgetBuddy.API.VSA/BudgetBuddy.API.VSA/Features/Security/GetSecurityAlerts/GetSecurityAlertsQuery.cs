using BudgetBuddy.API.VSA.Features.Security.GetSecurityEvents;

namespace BudgetBuddy.API.VSA.Features.Security.GetSecurityAlerts;

public record GetSecurityAlertsQuery() : IRequest<List<SecurityEventResponse>>;
