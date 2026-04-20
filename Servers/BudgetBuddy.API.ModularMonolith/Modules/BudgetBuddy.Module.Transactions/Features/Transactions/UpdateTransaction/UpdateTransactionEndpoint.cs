using Mapster;

namespace BudgetBuddy.Module.Transactions.Features.UpdateTransaction;

public class UpdateTransactionEndpoint : ICarterModule
{
    public record UpdateTransactionRequest(
    Guid? CategoryId,
    Guid? TypeId,
    decimal Amount,
    string CurrencyCode,
    decimal? RefCurrencyAmount,
    TransactionType TransactionType,
    PaymentType PaymentType,
    string? Note,
    LocalDate TransactionDate,
    string? Payee,
    string? Labels
);

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/transactions/{id:guid}", async (
            Guid id,
            UpdateTransactionRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = request.Adapt<UpdateTransactionCommand>() with { Id = id };

            var result = await mediator.Send(command, cancellationToken);

            return Results.Ok(result);
        })
        .WithSummary("Update a transaction")
        .WithDescription("Updates an existing transaction's details including amount, category, payment type, and transaction date.")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .WithTags("Transactions")
        .WithName("UpdateTransaction")
        ;
    }
}


