using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;
using BudgetBuddy.Shared.Infrastructure.Services;
using BudgetBuddy.Shared.Messages.Events.Investments;

namespace BudgetBuddy.Service.Investments.Features.UpdateInvestment;

public class UpdateInvestmentHandler(
    InvestmentsDbContext _context,
    IAccountOwnershipService _accountOwnershipService,
    IMapper _mapper,
    ICurrentUserService currentUserService,
    IDomainEventCollector _collector,
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
        {
            throw new NotFoundException(nameof(Investment), request.Id);
        }

        // If account is specified, verify it belongs to user
        if (request.AccountId.HasValue)
        {
            var accountExists = await _accountOwnershipService
                .AccountBelongsToUserAsync(request.AccountId.Value, UserId, cancellationToken);

            if (!accountExists)
            {
                throw new NotFoundException("Account", request.AccountId.Value);
            }
        }

        _mapper.Map(request, investment);

        _collector.Collect(new InvestmentUpdatedEvent
        {
            InvestmentId  = investment.Id,
            UserId        = UserId,
            Symbol        = investment.Symbol,
            Name          = investment.Name,
            Type          = investment.Type,
            Quantity      = investment.Quantity,
            PurchasePrice = investment.PurchasePrice,
            CurrencyCode  = investment.CurrencyCode,
            PurchaseDate  = investment.PurchaseDate,
        });

        await _context.SaveChangesAsync(cancellationToken);
        await _cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        _logger.LogInformation("Investment {InvestmentId} updated successfully", request.Id);

        return _mapper.Map<InvestmentResponse>(investment);
    }
}
