using Microsoft.AspNetCore.Mvc;

namespace BudgetBuddy.API.VSA.Features.Investments.BatchDeleteInvestments;

public class BatchDeleteInvestmentsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/investments/batch", async (
            [FromBody] BatchDeleteInvestmentsCommand command,
            ISender sender) =>
        {
            var result = await sender.Send(command);

            if (result.FailedCount == 0)
                return Results.Ok(result);

            if (result.SuccessCount == 0)
                return Results.BadRequest(result);

            return Results.Ok(result);
        })
        .WithSummary("Batch delete investments")
        .WithDescription("Deletes multiple investments in a single operation. Returns success and failure counts for partial operations.")
        .WithBatchOperationRateLimit()
        .RequireAuthorization()
        .WithTags("Investments")
        .WithName("BatchDeleteInvestments")
        .Produces<BatchDeleteInvestmentsResponse>(200)
        .Produces<BatchDeleteInvestmentsResponse>(400)
        ;
    }
}
