using BudgetBuddy.Application.Features.Transfers.CreateTransfer;
using BudgetBuddy.API.Endpoints;
using Mapster;

namespace BudgetBuddy.API.Mappings;

public class TransferMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateTransferRequest, CreateTransferCommand>()
            .MapWith(src => new CreateTransferCommand(
                src.FromAccountId,
                src.ToAccountId,
                src.Amount,
                src.CurrencyCode,
                src.PaymentType,
                src.Note,
                src.TransferDate
            ));
    }
}
