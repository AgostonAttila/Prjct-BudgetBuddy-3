namespace BudgetBuddy.Shared.Infrastructure.Behaviors;

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

        // S2139: intentional log-and-rethrow in both catch blocks — captures full request context
        // at the pipeline level before the exception is handled generically by GlobalExceptionHandler.
#pragma warning disable S2139
        try
        {
            var response = await next();

            logger.LogInformation("Handled {RequestName}", requestName);

            return response;
        }
        catch (FluentValidation.ValidationException ex)
        {
            // Log the request body (destructured via SensitiveDataDestructuringPolicy so PII is
            // redacted) together with the validation errors to speed up debugging without exposing
            // raw user data in log sinks (#21).
            logger.LogWarning(ex,
                "Validation failed for {RequestName} — {@Request} — Errors: {Errors}",
                requestName,
                request,
                ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
            throw;
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            // Request was cancelled (timeout, user navigation, etc.)
            logger.LogWarning(ex, "Request {RequestName} was cancelled", requestName);
            throw; // Re-throw to propagate cancellation up the stack
        }
#pragma warning restore S2139
    }
}
