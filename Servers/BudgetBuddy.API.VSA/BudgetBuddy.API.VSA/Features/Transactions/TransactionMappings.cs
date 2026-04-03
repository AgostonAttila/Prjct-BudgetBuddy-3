using BudgetBuddy.API.VSA.Features.Transactions.CreateTransaction;
using BudgetBuddy.API.VSA.Features.Transactions.UpdateTransaction;
using Mapster;
using static BudgetBuddy.API.VSA.Features.Transactions.UpdateTransaction.UpdateTransactionEndpoint;

namespace BudgetBuddy.API.VSA.Features.Transactions;

public class TransactionMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // CreateTransactionRequest -> CreateTransactionCommand
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

        // UpdateTransactionRequest -> UpdateTransactionCommand
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

        // CreateTransactionCommand -> Transaction
        config.NewConfig<CreateTransactionCommand, Transaction>()
            .Map(dest => dest.Id, src => Guid.NewGuid())
            .Map(dest => dest.CurrencyCode, src => src.CurrencyCode.ToUpperInvariant())
            .Ignore(dest => dest.CreatedAt)  // Handled by AuditableEntityInterceptor
            .Ignore(dest => dest.UpdatedAt)  // Handled by AuditableEntityInterceptor
            .Ignore(dest => dest.Account)    // Navigation property
            .Ignore(dest => dest.Category)   // Navigation property
            .Ignore(dest => dest.Type);      // Navigation property

        // UpdateTransactionCommand -> Transaction
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

        // Transaction -> CreateTransactionResponse
        config.NewConfig<Transaction, CreateTransaction.TransactionResponse>();

        // Transaction -> UpdateTransactionResponse
        config.NewConfig<Transaction, UpdateTransaction.TransactionResponse>();
    }
}
