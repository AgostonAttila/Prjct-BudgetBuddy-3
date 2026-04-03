using Microsoft.AspNetCore.Mvc;

namespace BudgetBuddy.API.VSA.Features.Transactions.BatchDeleteTransactions;

public class BatchDeleteTransactionsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/transactions/batch", async (
            [FromBody] BatchDeleteTransactionsCommand command,
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
        .WithSummary("Batch delete transactions")
        .WithDescription("Deletes multiple transactions in a single operation. Returns success and failure counts for partial operations.")
        .WithBatchOperationRateLimit()
        .RequireAuthorization()
        .WithTags("Transactions")
        .WithName("BatchDeleteTransactions")
        .Produces<BatchDeleteTransactionsResponse>(200)
        .Produces<BatchDeleteTransactionsResponse>(400)
        ;
    }
}
