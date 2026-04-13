using BudgetBuddy.Application.Features.Transactions.CreateTransaction;
using BudgetBuddy.Application.Features.Transactions.UpdateTransaction;
using BudgetBuddy.API.Endpoints;
using Mapster;

namespace BudgetBuddy.API.Mappings;

public class TransactionMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateTransactionRequest, CreateTransactionCommand>()
            .MapWith(src => new CreateTransactionCommand(
                src.AccountId,
                src.CategoryId,
                src.TypeId,
                src.Amount,
                src.CurrencyCode,
                src.RefCurrencyAmount,
                src.TransactionType,
                src.PaymentType,
                src.Note,
                src.TransactionDate,
                src.IsTransfer,
                src.TransferToAccountId,
                src.Payee,
                src.Labels
            ));

        config.NewConfig<UpdateTransactionRequest, UpdateTransactionCommand>()
            .MapWith(src => new UpdateTransactionCommand(
                Guid.Empty,
                src.CategoryId,
                src.TypeId,
                src.Amount,
                src.CurrencyCode,
                src.RefCurrencyAmount,
                src.TransactionType,
                src.PaymentType,
                src.Note,
                src.TransactionDate,
                src.Payee,
                src.Labels
            ));

        config.NewConfig<CreateTransactionCommand, Transaction>()
            .Map(dest => dest.Id, src => Guid.NewGuid())
            .Map(dest => dest.CurrencyCode, src => src.CurrencyCode.ToUpperInvariant())
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Account)
            .Ignore(dest => dest.Category)
            .Ignore(dest => dest.Type);

        config.NewConfig<UpdateTransactionCommand, Transaction>()
            .Map(dest => dest.CurrencyCode, src => src.CurrencyCode.ToUpperInvariant())
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.AccountId)
            .Ignore(dest => dest.IsTransfer)
            .Ignore(dest => dest.TransferToAccountId)
            .Ignore(dest => dest.UserId)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Account)
            .Ignore(dest => dest.Category)
            .Ignore(dest => dest.Type);

        config.NewConfig<Transaction, BudgetBuddy.Application.Features.Transactions.CreateTransaction.TransactionResponse>();

        config.NewConfig<Transaction, BudgetBuddy.Application.Features.Transactions.UpdateTransaction.TransactionResponse>();
    }
}
