namespace BudgetBuddy.Module.ReferenceData.Features.Currencies.DeleteCurrency;

public class DeleteCurrencyHandler(
    ReferenceDataDbContext context,
    IAccountOwnershipService accountOwnershipService,
    ILogger<DeleteCurrencyHandler> logger) : IRequestHandler<DeleteCurrencyCommand, Unit>
{
    public async Task<Unit> Handle(
        DeleteCurrencyCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting global currency {CurrencyId}", request.Id);

        var currency = await context.Currencies
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (currency == null)
            throw new NotFoundException(nameof(Currency), request.Id);

        // Check if currency is in use by any accounts
        var isInUse = await accountOwnershipService.IsCurrencyInUseAsync(currency.Code, cancellationToken);

        if (isInUse)
            throw new InvalidOperationException($"Cannot delete currency '{currency.Code}' as it is currently in use by one or more accounts.");

        context.Currencies.Remove(currency);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Currency {CurrencyId} deleted successfully", request.Id);

        return Unit.Value;
    }
}
