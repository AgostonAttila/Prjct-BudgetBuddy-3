using BudgetBuddy.Service.Transactions.Features.Transfers.CreateTransfer;
using Mapster;
using static BudgetBuddy.Service.Transactions.Features.Transfers.CreateTransfer.CreateTransferEndpoint;

namespace BudgetBuddy.Service.Transactions.Features.Transfers;


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