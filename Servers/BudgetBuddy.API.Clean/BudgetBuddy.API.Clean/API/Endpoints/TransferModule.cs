using BudgetBuddy.Application.Features.Transfers.CreateTransfer;
using Mapster;

namespace BudgetBuddy.API.Endpoints;

public record CreateTransferRequest(
    Guid FromAccountId, Guid ToAccountId, decimal Amount, string CurrencyCode,
    PaymentType PaymentType, string? Note, LocalDate TransferDate);

public class TransferModule : CarterModule
{
    public TransferModule() : base("/api/transfers")
    {
        WithTags("Transfers");
        RequireAuthorization();
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("", CreateTransfer)
            .WithSummary("Create a transfer between accounts")
            .WithDescription("Creates a transfer between two accounts by generating paired expense and income transactions. IMPORTANT: Always include an Idempotency-Key header to prevent duplicate transfers.")
            .WithIdempotency()
            .WithStrictRateLimit()
            .WithName("CreateTransfer");
    }

    private static async Task<IResult> CreateTransfer(CreateTransferRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        var command = request.Adapt<CreateTransferCommand>();
        var result = await mediator.Send(command, cancellationToken);
        return Results.Created($"/api/transfers/{result.FromTransactionId}", result);
    }
}
