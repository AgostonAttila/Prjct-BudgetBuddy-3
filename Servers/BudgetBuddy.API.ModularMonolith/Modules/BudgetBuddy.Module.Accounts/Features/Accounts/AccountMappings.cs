using BudgetBuddy.Module.Accounts.Features.CreateAccount;
using BudgetBuddy.Module.Accounts.Features.UpdateAccount;
using Mapster;
using static BudgetBuddy.Module.Accounts.Features.CreateAccount.CreateAccountEndpoint;

namespace BudgetBuddy.Module.Accounts.Features;

public class AccountMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        
        config.NewConfig<UpdateAccountRequest, UpdateAccountCommand>()
            .MapWith(src => new UpdateAccountCommand(
                Guid.Empty,
                src.Name,
                src.Description,
                src.DefaultCurrencyCode,
                src.InitialBalance
            ));
        
        // Request DTO -> Command mappings
        config.NewConfig<CreateAccountRequest, CreateAccountCommand>()
            .MapWith(src => new CreateAccountCommand(
                src.Name,
                src.Description,
                src.DefaultCurrencyCode,
                src.InitialBalance
            ));

        // Command -> Entity mappings
        config.NewConfig<CreateAccountCommand, Account>()
            .Map(dest => dest.Id, src => Guid.NewGuid())
            .Map(dest => dest.DefaultCurrencyCode, src => src.DefaultCurrencyCode.ToUpperInvariant())
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt);

        config.NewConfig<UpdateAccountCommand, Account>()
            .Map(dest => dest.DefaultCurrencyCode, src => src.DefaultCurrencyCode.ToUpperInvariant())
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt);

        config.NewConfig<Account, CreateAccountResponse>();

        config.NewConfig<Account, UpdateAccountResponse>();
    }
}
