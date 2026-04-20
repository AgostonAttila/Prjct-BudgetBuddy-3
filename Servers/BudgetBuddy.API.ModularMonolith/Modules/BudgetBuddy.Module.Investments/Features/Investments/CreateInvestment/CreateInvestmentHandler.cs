using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Infrastructure.Services;

namespace BudgetBuddy.Module.Investments.Features.CreateInvestment;

public class CreateInvestmentHandler(
    InvestmentsDbContext context,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IUserCacheInvalidator cacheInvalidator,
    ILogger<CreateInvestmentHandler> logger) : UserAwareHandler<CreateInvestmentCommand, CreateInvestmentResponse>(currentUserService)
{
    public override async Task<CreateInvestmentResponse> Handle(
        CreateInvestmentCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Creating investment {Symbol} ({Type}) for user {UserId}",
            request.Symbol,
            request.Type,
            UserId);

        // Check for duplicate investment (same symbol, date, quantity, and price)
        // NOTE: Disabled - GlobalExceptionHandler catches DbUpdateException from unique constraint (IX_Investments_Dedup)
        // var exists = await context.Investments
        //     .AnyAsync(i =>
        //         i.UserId == userId &&
        //         i.Symbol == request.Symbol &&
        //         i.PurchaseDate == request.PurchaseDate &&
        //         i.Quantity == request.Quantity &&
        //         i.PurchasePrice == request.PurchasePrice,
        //         cancellationToken);
        // if (exists)
        //     throw new ValidationException(
        //         $"An identical investment purchase for {request.Symbol} on {request.PurchaseDate} already exists.");

        var investment = mapper.Map<Investment>(request);
        investment.UserId = UserId;

        context.Investments.Add(investment);
        await context.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        logger.LogInformation("Investment {InvestmentId} created successfully", investment.Id);

        return mapper.Map<CreateInvestmentResponse>(investment);
    }
}
