using System.Security.Claims;

namespace BudgetBuddy.API.VSA.Common.Infrastructure;

/// <summary>
/// Implementation of ICurrentUserService that retrieves user information from HttpContext
/// </summary>
public class HttpContextCurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string GetCurrentUserId()
    {
        var userId = GetCurrentUserIdOrNull();

        return string.IsNullOrEmpty(userId) ? throw new UnauthorizedAccessException("User is not authenticated or user ID claim is missing") : userId;
    }
   
    public string? GetCurrentUserIdOrNull()
    {
        var httpContext = httpContextAccessor.HttpContext;

        return httpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
