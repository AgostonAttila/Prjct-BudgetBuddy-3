using NodaTime.Text;

namespace BudgetBuddy.Module.Transactions.Features.GetTransactions;

public class GetTransactionsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/transactions", async (
            IMediator mediator,
            Guid? accountId,
            Guid? categoryId,
            string? startDate,
            string? endDate,
            TransactionType? type,
            string? search,
            int pageNumber = 1,
            int pageSize = 50,
            CancellationToken cancellationToken = default) =>
        {
            // Parse dates if provided
            LocalDate? parsedStartDate = null;
            LocalDate? parsedEndDate = null;

            if (!string.IsNullOrEmpty(startDate))
            {
                var parseResult = LocalDatePattern.Iso.Parse(startDate);
                if (parseResult.Success)
                    parsedStartDate = parseResult.Value;
            }

            if (!string.IsNullOrEmpty(endDate))
            {
                var parseResult = LocalDatePattern.Iso.Parse(endDate);
                if (parseResult.Success)
                    parsedEndDate = parseResult.Value;
            }

            var query = new GetTransactionsQuery(
                accountId,
                categoryId,
                parsedStartDate,
                parsedEndDate,
                type,
                search,
                pageNumber,
                pageSize
            );

            var result = await mediator.Send(query, cancellationToken);

            return Results.Ok(result);
        })
        .WithSummary("Get all transactions")
        .WithDescription("Retrieves all transactions for the authenticated user with optional filtering by account, category, date range, and transaction type.")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .CacheOutput("transactions") //  PII fields (Note, Payee) excluded from TransactionDto
        .WithTags("Transactions")
        .WithName("GetTransactions")
        ;
    }
}
