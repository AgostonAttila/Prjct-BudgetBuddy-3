namespace BudgetBuddy.Shared.Infrastructure;

public class HttpContextCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Keycloak: 'sub' claim = user UUID
    public string UserId => _httpContextAccessor.HttpContext?.User
        .FindFirst("sub")?.Value ?? string.Empty;

    public string UserName => _httpContextAccessor.HttpContext?.User
        .FindFirst("preferred_username")?.Value ?? string.Empty;

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) =>
        _httpContextAccessor.HttpContext?.User.IsInRole(role) ?? false;

    public string GetCurrentUserId()
    {
        var userId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        return userId;
    }

    public string? GetCurrentUserIdOrNull() =>
        _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
}
