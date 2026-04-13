using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Investments.UpdateInvestment;

public class UpdateInvestmentHandler(
    IInvestmentRepository investmentRepo,
    IAccountRepository accountRepo,
    IUnitOfWork uow,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IUserCacheInvalidator cacheInvalidator,
    ILogger<UpdateInvestmentHandler> logger) : UserAwareHandler<UpdateInvestmentCommand, InvestmentResponse>(currentUserService)
{
    public override async Task<InvestmentResponse> Handle(
        UpdateInvestmentCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating investment {InvestmentId} for user {UserId}", request.Id, UserId);

        var investment = await investmentRepo.GetByIdAsync(request.Id, UserId, cancellationToken);

        if (investment == null)
            throw new NotFoundException(nameof(Investment), request.Id);

        // If account is specified, verify it belongs to user
        if (request.AccountId.HasValue)
        {
            var account = await accountRepo.GetByIdAsync(request.AccountId.Value, UserId, cancellationToken);

            if (account == null)
                throw new NotFoundException(nameof(Account), request.AccountId.Value);
        }

        // NOTE: Duplicate check disabled - GlobalExceptionHandler catches DbUpdateException from unique constraint (IX_Investments_Dedup)

        mapper.Map(request, investment);

        await uow.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        logger.LogInformation("Investment {InvestmentId} updated successfully", request.Id);

        return mapper.Map<InvestmentResponse>(investment);
    }
}
