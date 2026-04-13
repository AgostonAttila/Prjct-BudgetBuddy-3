using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Investments.CreateInvestment;

public class CreateInvestmentHandler(
    IInvestmentRepository investmentRepo,
    IUnitOfWork uow,
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

        // NOTE: Duplicate check disabled - GlobalExceptionHandler catches DbUpdateException from unique constraint (IX_Investments_Dedup)

        var investment = mapper.Map<Investment>(request);
        investment.UserId = UserId;

        await investmentRepo.AddAsync(investment, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        logger.LogInformation("Investment {InvestmentId} created successfully", investment.Id);

        return mapper.Map<CreateInvestmentResponse>(investment);
    }
}
