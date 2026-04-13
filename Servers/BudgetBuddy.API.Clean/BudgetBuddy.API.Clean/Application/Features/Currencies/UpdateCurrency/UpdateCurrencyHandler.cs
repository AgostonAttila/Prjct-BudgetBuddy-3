using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Currencies.UpdateCurrency;

public class UpdateCurrencyHandler(
    ICurrencyRepository currencyRepo,
    IUnitOfWork uow,
    IMapper mapper,
    ILogger<UpdateCurrencyHandler> logger) : IRequestHandler<UpdateCurrencyCommand, CurrencyResponse>
{
    public async Task<CurrencyResponse> Handle(
        UpdateCurrencyCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating global currency {CurrencyId}", request.Id);

        var currency = await currencyRepo.GetByIdAsync(request.Id, cancellationToken);

        if (currency == null)
            throw new NotFoundException(nameof(Currency), request.Id);

        var codeExists = await currencyRepo.ExistsByCodeAsync(request.Code, excludeId: request.Id, ct: cancellationToken);

        if (codeExists)
            throw new InvalidOperationException($"Currency with code '{request.Code}' already exists.");

        currency.Code = request.Code.ToUpperInvariant();
        currency.Symbol = request.Symbol;
        currency.Name = request.Name;

        await uow.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Currency {CurrencyId} updated successfully", request.Id);

        return mapper.Map<CurrencyResponse>(currency);
    }
}
