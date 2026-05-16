using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;
using BudgetBuddy.Shared.Infrastructure.Services;
using BudgetBuddy.Shared.Messages.Events.Investments;

namespace BudgetBuddy.Service.Investments.Features.CreateInvestment;

public class CreateInvestmentHandler(
    InvestmentsDbContext context,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IDomainEventCollector collector,
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

        var investment = mapper.Map<Investment>(request);
        investment.UserId = UserId;

        collector.Collect(new InvestmentCreatedEvent
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

        context.Investments.Add(investment);
        await context.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        logger.LogInformation("Investment {InvestmentId} created successfully", investment.Id);

        return mapper.Map<CreateInvestmentResponse>(investment);
    }
}
