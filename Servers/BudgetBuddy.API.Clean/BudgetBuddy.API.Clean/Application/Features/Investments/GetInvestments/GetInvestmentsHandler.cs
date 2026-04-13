using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Investments.GetInvestments;

public class GetInvestmentsHandler(
    IInvestmentRepository investmentRepo,
    ICurrentUserService currentUserService,
    ILogger<GetInvestmentsHandler> logger) : UserAwareHandler<GetInvestmentsQuery, GetInvestmentsResponse>(currentUserService)
{
    public override async Task<GetInvestmentsResponse> Handle(
        GetInvestmentsQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching investments for user {UserId}", UserId);

        var filter = new InvestmentFilter(UserId, request.Type, request.SearchTerm, request.PageNumber, request.PageSize);
        var (items, totalCount) = await investmentRepo.GetPagedAsync(filter, cancellationToken);

        var dtos = items
            .Select(i => new InvestmentDto(
                i.Id,
                i.Symbol,
                i.Name,
                i.Type,
                i.Quantity,
                i.PurchasePrice,
                i.CurrencyCode,
                i.PurchaseDate,
                i.Note,
                i.AccountName))
            .ToList();

        logger.LogInformation(
            "Found {Count} investments (total {TotalCount}) for user {UserId}",
            dtos.Count,
            totalCount,
            UserId);

        return new GetInvestmentsResponse(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}
