using BudgetBuddy.Application.Features.Security.GetSecurityEvents;

namespace BudgetBuddy.Application.Features.Security.GetSecurityAlerts;

public record GetSecurityAlertsQuery() : IRequest<List<SecurityEventResponse>>;
