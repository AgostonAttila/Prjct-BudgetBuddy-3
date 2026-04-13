
namespace BudgetBuddy.Application.Common.Behaviors;

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

        // ⚠️ SECURITY: Intentionally log only the request TYPE NAME, never the request/response body.
        // Commands and queries may contain PII (e.g. Transaction.Payee, Transaction.Note, passwords).
        // If structured request logging is ever needed, use Destructurama.Attributed + [NotLogged]
        // on sensitive properties — DO NOT use {@Request} destructuring without redaction.
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
