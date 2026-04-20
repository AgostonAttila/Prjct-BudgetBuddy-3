namespace BudgetBuddy.Module.ReferenceData.Features.Currencies.UpdateCurrency;

public class UpdateCurrencyHandler(
    ReferenceDataDbContext context,
    IMapper mapper,
    ILogger<UpdateCurrencyHandler> logger) : IRequestHandler<UpdateCurrencyCommand, CurrencyResponse>
{
    public async Task<CurrencyResponse> Handle(
        UpdateCurrencyCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating global currency {CurrencyId}", request.Id);

        var currency = await context.Currencies
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (currency == null)
            throw new NotFoundException(nameof(Currency), request.Id);

        // Check if new code conflicts with another currency
        var codeExists = await context.Currencies
            .AnyAsync(c => c.Code == request.Code.ToUpperInvariant() && c.Id != request.Id, cancellationToken);

        if (codeExists)
            throw new InvalidOperationException($"Currency with code '{request.Code}' already exists.");

        currency.Code = request.Code.ToUpperInvariant();
        currency.Symbol = request.Symbol;
        currency.Name = request.Name;

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Currency {CurrencyId} updated successfully", request.Id);

        return mapper.Map<CurrencyResponse>(currency);
    }
}
