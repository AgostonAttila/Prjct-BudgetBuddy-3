using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Infrastructure.Services;

namespace BudgetBuddy.Module.Investments.Features.UpdateInvestment;

public class UpdateInvestmentHandler(
    InvestmentsDbContext _context,
    IAccountOwnershipService _accountOwnershipService,
    IMapper _mapper,
    ICurrentUserService currentUserService,
    IUserCacheInvalidator _cacheInvalidator,
    ILogger<UpdateInvestmentHandler> _logger) : UserAwareHandler<UpdateInvestmentCommand, InvestmentResponse>(currentUserService)
{
    public override async Task<InvestmentResponse> Handle(
        UpdateInvestmentCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating investment {InvestmentId} for user {UserId}", request.Id, UserId);

        var investment = await _context.Investments
            .FirstOrDefaultAsync(i => i.Id == request.Id && i.UserId == UserId, cancellationToken);

        if (investment == null)
            throw new NotFoundException(nameof(Investment), request.Id);

        // If account is specified, verify it belongs to user
        if (request.AccountId.HasValue)
        {
            var accountExists = await _accountOwnershipService
                .AccountBelongsToUserAsync(request.AccountId.Value, UserId, cancellationToken);

            if (!accountExists)
                throw new NotFoundException("Account", request.AccountId.Value);
        }

        // Check for duplicate investment (same symbol, date, quantity, and price, excluding current)
        // NOTE: Disabled - GlobalExceptionHandler catches DbUpdateException from unique constraint (IX_Investments_Dedup)
        // var exists = await _context.Investments
        //     .AnyAsync(i =>
        //         i.UserId == userId &&
        //         i.Symbol == request.Symbol &&
        //         i.PurchaseDate == request.PurchaseDate &&
        //         i.Quantity == request.Quantity &&
        //         i.PurchasePrice == request.PurchasePrice &&
        //         i.Id != request.Id,
        //         cancellationToken);
        // if (exists)
        //     throw new ValidationException(
        //         $"An identical investment purchase for {request.Symbol} on {request.PurchaseDate} already exists.");

        _mapper.Map(request, investment);

        await _context.SaveChangesAsync(cancellationToken);
        await _cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        _logger.LogInformation("Investment {InvestmentId} updated successfully", request.Id);

        return _mapper.Map<InvestmentResponse>(investment);
    }
}
