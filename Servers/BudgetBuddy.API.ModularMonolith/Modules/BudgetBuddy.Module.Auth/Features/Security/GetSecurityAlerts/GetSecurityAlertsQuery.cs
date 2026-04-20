using BudgetBuddy.Module.Auth.Features.Security.GetSecurityEvents;

namespace BudgetBuddy.Module.Auth.Features.Security.GetSecurityAlerts;

public record GetSecurityAlertsQuery() : IRequest<List<SecurityEventResponse>>;
