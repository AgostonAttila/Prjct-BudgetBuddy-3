using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Currencies.DeleteCurrency;

public class DeleteCurrencyHandler(
    ICurrencyRepository currencyRepo,
    IAccountRepository accountRepo,
    IUnitOfWork uow,
    ILogger<DeleteCurrencyHandler> logger) : IRequestHandler<DeleteCurrencyCommand, Unit>
{
    public async Task<Unit> Handle(
        DeleteCurrencyCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting global currency {CurrencyId}", request.Id);

        var currency = await currencyRepo.GetByIdAsync(request.Id, cancellationToken);

        if (currency == null)
            throw new NotFoundException(nameof(Currency), request.Id);

        var isInUse = await accountRepo.HasDefaultCurrencyAsync(currency.Code, cancellationToken);

        if (isInUse)
            throw new InvalidOperationException($"Cannot delete currency '{currency.Code}' as it is currently in use by one or more accounts.");

        currencyRepo.Remove(currency);
        await uow.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Currency {CurrencyId} deleted successfully", request.Id);

        return Unit.Value;
    }
}
