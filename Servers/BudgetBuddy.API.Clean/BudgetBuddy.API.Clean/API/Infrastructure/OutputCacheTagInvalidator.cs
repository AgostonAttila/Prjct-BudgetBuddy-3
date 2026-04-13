using BudgetBuddy.Application.Common.Contracts;
using Microsoft.AspNetCore.OutputCaching;

namespace BudgetBuddy.API.Infrastructure;

public class OutputCacheTagInvalidator(IOutputCacheStore outputCacheStore) : ICacheTagInvalidator
{
    public async Task EvictByTagAsync(string tag, CancellationToken cancellationToken)
        => await outputCacheStore.EvictByTagAsync(tag, cancellationToken);
}
