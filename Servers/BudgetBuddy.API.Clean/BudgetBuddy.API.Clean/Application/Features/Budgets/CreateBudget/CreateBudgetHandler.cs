using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Budgets.CreateBudget;

public class CreateBudgetHandler(
    IBudgetRepository budgetRepo,
    ICategoryRepository categoryRepo,
    IUnitOfWork uow,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IUserCacheInvalidator cacheInvalidator,
    ILogger<CreateBudgetHandler> logger) : UserAwareHandler<CreateBudgetCommand, BudgetResponse>(currentUserService)
{
    public override async Task<BudgetResponse> Handle(
        CreateBudgetCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating budget for user {UserId}", UserId);

        var category = await categoryRepo.GetByIdAsync(request.CategoryId, UserId, cancellationToken);

        if (category == null)
            throw new NotFoundException(nameof(Category), request.CategoryId);

        if (await budgetRepo.ExistsForMonthAsync(UserId, request.CategoryId, request.Year, request.Month, cancellationToken))
            throw new DomainValidationException($"Budget already exists for category '{category.Name}' in {request.Year}-{request.Month:D2}");

        var budget = mapper.Map<Budget>(request);
        budget.UserId = UserId;
        budget.Category = category;

        await budgetRepo.AddAsync(budget, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        logger.LogInformation("Budget {BudgetId} created successfully", budget.Id);

        return mapper.Map<BudgetResponse>(budget);
    }
}
