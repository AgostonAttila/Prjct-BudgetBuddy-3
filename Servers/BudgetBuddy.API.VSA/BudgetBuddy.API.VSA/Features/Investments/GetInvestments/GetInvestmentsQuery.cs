namespace BudgetBuddy.API.VSA.Features.Investments.GetInvestments;

public record GetInvestmentsQuery(
    InvestmentType? Type = null,
    string? SearchTerm = null, // Search in Symbol, Name, Note
    int PageNumber = 1,
    int PageSize = 50
) : IRequest<GetInvestmentsResponse>;

public record GetInvestmentsResponse(
    List<InvestmentDto> Investments,
    int TotalCount,
    int PageNumber,
    int PageSize
);

public record InvestmentDto(
    Guid Id,
    string Symbol,
    string Name,
    InvestmentType Type,
    decimal Quantity,
    decimal PurchasePrice,
    string CurrencyCode,
    LocalDate PurchaseDate,
    string? Note,
    string? AccountName
);
