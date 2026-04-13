namespace BudgetBuddy.Application.Common.Handlers;

/// <summary>
/// Base handler that resolves the current user ID once, eliminating the
/// var userId = currentUserService.GetCurrentUserId() boilerplate from every handler.
/// </summary>
public abstract class UserAwareHandler<TRequest, TResponse>(ICurrentUserService currentUserService)
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    protected string UserId => currentUserService.GetCurrentUserId();

    public abstract Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
