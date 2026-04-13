using System.Security.Claims;

namespace BudgetBuddy.API.Filters;

public class UserIdEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        context.HttpContext.Items["UserId"] = userId;

        return await next(context);
    }
}