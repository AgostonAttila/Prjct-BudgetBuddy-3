namespace BudgetBuddy.Application.Features.Currencies.GetCurrencies;

public record GetCurrenciesQuery() : IRequest<List<CurrencyDto>>;

public record CurrencyDto(
    Guid Id,
    string Code,
    string Symbol,
    string Name
);
