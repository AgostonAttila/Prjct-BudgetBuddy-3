using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Currencies.CreateCurrency;

public class CreateCurrencyHandler(
    ICurrencyRepository currencyRepo,
    IUnitOfWork uow,
    IMapper mapper,
    ILogger<CreateCurrencyHandler> logger) : IRequestHandler<CreateCurrencyCommand, CreateCurrencyResponse>
{
    public async Task<CreateCurrencyResponse> Handle(
        CreateCurrencyCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating global currency {CurrencyCode}", request.Code);

        var exists = await currencyRepo.ExistsByCodeAsync(request.Code, ct: cancellationToken);

        if (exists)
            throw new InvalidOperationException($"Currency with code '{request.Code}' already exists.");

        var currency = mapper.Map<Currency>(request);

        currencyRepo.Add(currency);
        await uow.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Currency {CurrencyId} created successfully", currency.Id);

        return mapper.Map<CreateCurrencyResponse>(currency);
    }
}
