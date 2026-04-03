using BudgetBuddy.API.VSA.Features.Transfers.CreateTransfer;
using Mapster;
using static BudgetBuddy.API.VSA.Features.Transfers.CreateTransfer.CreateTransferEndpoint;

namespace BudgetBuddy.API.VSA.Features.Transfers;


public class TransferMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // CreateTransferRequest -> CreateTransferCommand
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