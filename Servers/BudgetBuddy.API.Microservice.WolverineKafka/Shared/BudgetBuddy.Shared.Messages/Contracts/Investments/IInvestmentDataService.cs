using BudgetBuddy.Shared.Kernel.Enums;
using NodaTime;

namespace BudgetBuddy.Shared.Messages.Contracts.Investments;

public record InvestmentDataItem(
    Guid Id,
    string Symbol,
    string Name,
    InvestmentType Type,
    decimal Quantity,
    decimal PurchasePrice,
    LocalDate PurchaseDate,
    string CurrencyCode);

public record ExchangeRateSnapshotItem(
    string Currency,
    LocalDate Date,
    decimal RateToUsd);

public interface IInvestmentDataService
{
    Task<List<InvestmentDataItem>> GetActiveInvestmentsAsync(
        string userId,
        LocalDate? startDate,
        LocalDate? endDate,
        InvestmentType? type,
        CancellationToken cancellationToken = default);

    Task<List<ExchangeRateSnapshotItem>> GetExchangeRateSnapshotsAsync(
        LocalDate from,
        LocalDate to,
        CancellationToken cancellationToken = default);
}
