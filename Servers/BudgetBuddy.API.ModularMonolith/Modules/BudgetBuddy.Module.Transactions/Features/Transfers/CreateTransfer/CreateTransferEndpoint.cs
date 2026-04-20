using Mapster;

namespace BudgetBuddy.Module.Transactions.Features.Transfers.CreateTransfer;

public class CreateTransferEndpoint : ICarterModule
{
    public record CreateTransferRequest(
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    string CurrencyCode,
    PaymentType PaymentType,
    string? Note,
    LocalDate TransferDate);

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/transfers", async (
            CreateTransferRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = request.Adapt<CreateTransferCommand>();

            var result = await mediator.Send(command, cancellationToken);

            return Results.Created($"/api/transfers/{result.FromTransactionId}", result);
        })
        .WithSummary("Create a transfer between accounts")
        .WithDescription("Creates a transfer between two accounts by generating paired expense and income transactions. IMPORTANT: Always include an Idempotency-Key header to prevent duplicate transfers.")
        .WithIdempotency()  // CRITICAL: Prevent duplicate transfers
        .WithStrictRateLimit()
        .RequireAuthorization()
        .WithTags("Transfers")
        .WithName("CreateTransfer")
        ;
    }
}


