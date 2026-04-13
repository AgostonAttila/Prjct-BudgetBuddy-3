using BudgetBuddy.Application.Common.Contracts;
using MediatR;

namespace BudgetBuddy.Application.Common.Behaviors;

public class CacheInvalidationBehavior<TRequest, TResponse>(
    ICacheTagInvalidator cacheTagInvalidator,
    ILogger<CacheInvalidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Execute command first
        var response = await next();

        // Invalidate cache if command implements ICacheInvalidator
        if (request is ICacheInvalidator cacheInvalidator)
        {
            var requestName = typeof(TRequest).Name;

            foreach (var tag in cacheInvalidator.CacheTags)
            {
                logger.LogInformation(
                    "Invalidating cache tag '{CacheTag}' after {RequestName}",
                    tag, requestName);

                await cacheTagInvalidator.EvictByTagAsync(tag, cancellationToken);
            }

            if (cacheInvalidator.CacheTags.Length > 0)
            {
                logger.LogInformation(
                    "Invalidated {Count} cache tag(s) after {RequestName}: {Tags}",
                    cacheInvalidator.CacheTags.Length,
                    requestName,
                    string.Join(", ", cacheInvalidator.CacheTags));
            }
        }

        return response;
    }
}
