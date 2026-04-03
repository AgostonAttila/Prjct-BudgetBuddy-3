using Mapster;

namespace BudgetBuddy.API.VSA.Features.Transactions.CreateTransaction;

public class CreateTransactionEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/transactions", async (
            CreateTransactionRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = request.Adapt<CreateTransactionCommand>();

            var result = await mediator.Send(command, cancellationToken);

            return Results.Created($"/api/transactions/{result.Id}", result);
        })
        .WithSummary("Create a new transaction")
        .WithDescription("Creates a new financial transaction. Include an Idempotency-Key header to prevent duplicate transactions on retry.")
        .WithIdempotency()  // Prevent duplicate transactions
        .WithStandardRateLimit()
        .RequireAuthorization()
        .WithTags("Transactions")
        .WithName("CreateTransaction")
        ;
    }
}

public record CreateTransactionRequest(
    Guid AccountId,
    Guid? CategoryId,
    Guid? TypeId,
    decimal Amount,
    string CurrencyCode,
    decimal? RefCurrencyAmount,
    TransactionType TransactionType,
    PaymentType PaymentType,
    string? Note,
    LocalDate TransactionDate,
    bool IsTransfer = false,
    Guid? TransferToAccountId = null,
    string? Payee = null,
    string? Labels = null
);
