using BudgetBuddy.API.VSA.Features.Investments.CreateInvestment;
using BudgetBuddy.API.VSA.Features.Investments.UpdateInvestment;
using Mapster;

namespace BudgetBuddy.API.VSA.Features.Investments;

public class InvestmentMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // CreateInvestmentRequest -> CreateInvestmentCommand
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

        // UpdateInvestmentRequest -> UpdateInvestmentCommand
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

        // CreateInvestmentCommand -> Investment
        config.NewConfig<CreateInvestmentCommand, Investment>()
            .Map(dest => dest.Id, src => Guid.NewGuid())
            .Map(dest => dest.CurrencyCode, src => src.CurrencyCode.ToUpperInvariant())
            .Map(dest => dest.Symbol, src => src.Symbol.ToUpperInvariant())
            .Ignore(dest => dest.CreatedAt)  // Handled by AuditableEntityInterceptor
            .Ignore(dest => dest.UpdatedAt)  // Handled by AuditableEntityInterceptor
            .Ignore(dest => dest.Account);   // Navigation property

        // UpdateInvestmentCommand -> Investment
        config.NewConfig<UpdateInvestmentCommand, Investment>()
            .Map(dest => dest.CurrencyCode, src => src.CurrencyCode.ToUpperInvariant())
            .Map(dest => dest.Symbol, src => src.Symbol.ToUpperInvariant())
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.UserId)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Account);

        // Investment -> CreateInvestmentResponse
        config.NewConfig<Investment, CreateInvestmentResponse>();

        // Investment -> InvestmentResponse (UpdateInvestment response)
        config.NewConfig<Investment, UpdateInvestment.InvestmentResponse>();
    }
}
