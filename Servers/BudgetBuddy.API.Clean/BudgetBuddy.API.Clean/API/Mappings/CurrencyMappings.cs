using BudgetBuddy.Application.Features.Currencies.CreateCurrency;
using BudgetBuddy.Application.Features.Currencies.UpdateCurrency;
using BudgetBuddy.API.Endpoints;
using Mapster;

namespace BudgetBuddy.API.Mappings;

public class CurrencyMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateCurrencyRequest, CreateCurrencyCommand>()
            .MapWith(src => new CreateCurrencyCommand(
                src.Code,
                src.Symbol,
                src.Name
            ));

        config.NewConfig<UpdateCurrencyRequest, UpdateCurrencyCommand>()
            .MapWith(src => new UpdateCurrencyCommand(
                Guid.Empty,
                src.Code,
                src.Symbol,
                src.Name
            ));

        config.NewConfig<CreateCurrencyCommand, Currency>()
            .Map(dest => dest.Id, src => Guid.NewGuid())
            .Map(dest => dest.Code, src => src.Code.ToUpperInvariant())
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt);

        config.NewConfig<UpdateCurrencyCommand, Currency>()
            .Map(dest => dest.Code, src => src.Code.ToUpperInvariant())
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt);

        config.NewConfig<Currency, CreateCurrencyResponse>();

        config.NewConfig<Currency, BudgetBuddy.Application.Features.Currencies.UpdateCurrency.CurrencyResponse>();
    }
}
