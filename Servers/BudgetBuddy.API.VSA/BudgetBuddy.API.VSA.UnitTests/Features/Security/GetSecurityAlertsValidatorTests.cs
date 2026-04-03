using BudgetBuddy.API.VSA.Features.Security.GetSecurityAlerts;
using FluentAssertions;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Security;

// GetSecurityAlertsQuery has no FluentValidation validator — these tests verify the query can be constructed.
public class GetSecurityAlertsValidatorTests
{
    [Fact]
    public void GetSecurityAlertsQuery_CanBeInstantiated()
    {
        var query = new GetSecurityAlertsQuery();
        query.Should().NotBeNull();
    }
}
