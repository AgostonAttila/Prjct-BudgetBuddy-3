using BudgetBuddy.API.VSA.Features.Currencies.CreateCurrency;
using BudgetBuddy.API.VSA.Features.Currencies.UpdateCurrency;
using Mapster;
using static BudgetBuddy.API.VSA.Features.Currencies.CreateCurrency.CreateCurrencyEndpoint;

namespace BudgetBuddy.API.VSA.Features.Currencies;

public class CurrencyMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // CreateCurrencyRequest -> CreateCurrencyCommand
        config.NewConfig<CreateCurrencyRequest, CreateCurrencyCommand>()
            .MapWith(src => new CreateCurrencyCommand(
                src.Code,
                src.Symbol,
                src.Name
            ));

        // UpdateCurrencyRequest -> UpdateCurrencyCommand
        config.NewConfig<UpdateCurrencyRequest, UpdateCurrencyCommand>()
            .MapWith(src => new UpdateCurrencyCommand(
                Guid.Empty, 
                src.Code,
                src.Symbol,
                src.Name
            ));

        // CreateCurrencyCommand -> Currency
        config.NewConfig<CreateCurrencyCommand, Currency>()
            .Map(dest => dest.Id, src => Guid.NewGuid())
            .Map(dest => dest.Code, src => src.Code.ToUpperInvariant())
            .Ignore(dest => dest.CreatedAt)  // Handled by AuditableEntityInterceptor
            .Ignore(dest => dest.UpdatedAt);

        // UpdateCurrencyCommand -> Currency
        config.NewConfig<UpdateCurrencyCommand, Currency>()
            .Map(dest => dest.Code, src => src.Code.ToUpperInvariant())
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt);

        // Currency -> CreateCurrencyResponse
        config.NewConfig<Currency, CreateCurrencyResponse>();

        // Currency -> CurrencyResponse (UpdateCurrency response)
        config.NewConfig<Currency, UpdateCurrency.CurrencyResponse>();
    }
}
