
namespace BudgetBuddy.API.VSA.Common.Shared.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("Handling {RequestName}", requestName);

        try
        {
            var response = await next();

            logger.LogInformation("Handled {RequestName}", requestName);

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Request was cancelled (timeout, user navigation, etc.)
            logger.LogWarning("Request {RequestName} was cancelled", requestName);
            throw; // Re-throw to propagate cancellation up the stack
        }
    }
}
