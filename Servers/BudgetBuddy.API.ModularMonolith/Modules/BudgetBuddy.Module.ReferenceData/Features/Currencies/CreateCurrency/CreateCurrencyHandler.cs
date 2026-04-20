namespace BudgetBuddy.Module.ReferenceData.Features.Currencies.CreateCurrency;

public class CreateCurrencyHandler(
    ReferenceDataDbContext context,
    IMapper mapper,
    ILogger<CreateCurrencyHandler> logger) : IRequestHandler<CreateCurrencyCommand, CreateCurrencyResponse>
{
    public async Task<CreateCurrencyResponse> Handle(
        CreateCurrencyCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating global currency {CurrencyCode}", request.Code);

        // Check if currency code already exists
        var exists = await context.Currencies
            .AnyAsync(c => c.Code == request.Code.ToUpperInvariant(), cancellationToken);

        if (exists)
            throw new InvalidOperationException($"Currency with code '{request.Code}' already exists.");

        var currency = mapper.Map<Currency>(request);

        context.Currencies.Add(currency);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Currency {CurrencyId} created successfully", currency.Id);

        return mapper.Map<CreateCurrencyResponse>(currency);
    }
}
