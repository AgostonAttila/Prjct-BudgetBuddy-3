namespace BudgetBuddy.API.VSA.Features.Transfers.CreateTransfer;

public record CreateTransferCommand(
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    string CurrencyCode,
    PaymentType PaymentType,
    string? Note,
    LocalDate TransferDate
) : IRequest<TransferResponse>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.Transactions, Tags.AccountBalance, Tags.Dashboard, Tags.MonthlySummary, Tags.IncomeVsExpense];
}

public record TransferResponse(
    Guid FromTransactionId,
    Guid ToTransactionId,
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    string CurrencyCode,
    LocalDate TransferDate
);
