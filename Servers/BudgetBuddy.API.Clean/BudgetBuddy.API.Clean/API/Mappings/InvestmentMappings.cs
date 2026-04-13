using BudgetBuddy.Application.Features.Investments.CreateInvestment;
using BudgetBuddy.Application.Features.Investments.UpdateInvestment;
using BudgetBuddy.API.Endpoints;
using Mapster;

namespace BudgetBuddy.API.Mappings;

public class InvestmentMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateInvestmentRequest, CreateInvestmentCommand>()
            .MapWith(src => new CreateInvestmentCommand(
                src.Symbol,
                src.Name,
                src.Type,
                src.Quantity,
                src.PurchasePrice,
                src.CurrencyCode,
                src.PurchaseDate,
                src.Note,
                src.AccountId
            ));

        config.NewConfig<UpdateInvestmentRequest, UpdateInvestmentCommand>()
            .MapWith(src => new UpdateInvestmentCommand(
                Guid.Empty,
                src.Symbol,
                src.Name,
                src.Type,
                src.Quantity,
                src.PurchasePrice,
                src.CurrencyCode,
                src.PurchaseDate,
                src.Note,
                src.AccountId
            ));

        config.NewConfig<CreateInvestmentCommand, Investment>()
            .Map(dest => dest.Id, src => Guid.NewGuid())
            .Map(dest => dest.CurrencyCode, src => src.CurrencyCode.ToUpperInvariant())
            .Map(dest => dest.Symbol, src => src.Symbol.ToUpperInvariant())
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Account);

        config.NewConfig<UpdateInvestmentCommand, Investment>()
            .Map(dest => dest.CurrencyCode, src => src.CurrencyCode.ToUpperInvariant())
            .Map(dest => dest.Symbol, src => src.Symbol.ToUpperInvariant())
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.UserId)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Account);

        config.NewConfig<Investment, CreateInvestmentResponse>();

        config.NewConfig<Investment, BudgetBuddy.Application.Features.Investments.UpdateInvestment.InvestmentResponse>();
    }
}
