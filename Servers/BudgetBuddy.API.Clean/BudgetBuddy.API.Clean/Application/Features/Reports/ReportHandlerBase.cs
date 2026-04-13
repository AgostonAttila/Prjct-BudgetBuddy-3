
namespace BudgetBuddy.Application.Features.Reports;

/// <summary>
/// Eliminates userId + displayCurrency boilerplate repeated in every report handler.
/// </summary>
public abstract class ReportHandlerBase<TQuery, TResponse>(
    ICurrentUserService currentUserService,
    IUserCurrencyService userCurrencyService,
    ILogger logger) : IRequestHandler<TQuery, TResponse>
    where TQuery : IRequest<TResponse>
{
    protected readonly ILogger Logger = logger;

    public async Task<TResponse> Handle(TQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var displayCurrency = await userCurrencyService.GetDisplayCurrencyAsync(
            GetDisplayCurrency(request), cancellationToken);

        return await HandleCoreAsync(userId, displayCurrency, request, cancellationToken);
    }

    /// <summary>Returns the display currency preference from the query (may be null).</summary>
    protected abstract string? GetDisplayCurrency(TQuery request);

    /// <summary>Performs the actual report computation and returns the response.</summary>
    protected abstract Task<TResponse> HandleCoreAsync(
        string userId,
        string displayCurrency,
        TQuery request,
        CancellationToken cancellationToken);
}
