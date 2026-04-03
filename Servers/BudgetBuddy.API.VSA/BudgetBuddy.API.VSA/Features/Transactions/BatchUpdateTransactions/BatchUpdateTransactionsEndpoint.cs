using Microsoft.AspNetCore.Mvc;

namespace BudgetBuddy.API.VSA.Features.Transactions.BatchUpdateTransactions;

public class BatchUpdateTransactionsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/transactions/batch", async (
            [FromBody] BatchUpdateTransactionsCommand command,
            ISender sender) =>
        {
            var result = await sender.Send(command);

            if (result.FailedCount == 0)
            {
                return Results.Ok(result);
            }

            if (result.SuccessCount == 0)
            {
                return Results.BadRequest(result);
            }

            // Partial success
            return Results.Ok(result);
        })
        .WithSummary("Batch update transactions")
        .WithDescription("Updates multiple transactions in a single operation. Returns success and failure counts for partial operations.")
        .WithBatchOperationRateLimit()
        .RequireAuthorization()
        .WithTags("Transactions")
        .WithName("BatchUpdateTransactions")
        .Produces<BatchUpdateTransactionsResponse>(200)
        .Produces<BatchUpdateTransactionsResponse>(400)
        ;
    }
}
