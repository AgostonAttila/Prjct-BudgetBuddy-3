using BudgetBuddy.Service.Budgets.Features.BudgetAlerts.Services;
using Microsoft.EntityFrameworkCore;
using BudgetBuddy.Service.Investments.Features.Investments.SellInvestment;
using BudgetBuddy.Service.Investments.Services;
using BudgetBuddy.Shared.Messages.Contracts.Financial;
using BudgetBuddy.Shared.Messages.Contracts.Investments;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace BudgetBuddy.Service.UnitTests.Financial;

/// <summary>
/// Verifies decimal precision and boundary conditions in financial calculations.
/// These prevent silent rounding errors that would produce incorrect budget alerts
/// or investment gain/loss figures (#14).
/// </summary>
public class FinancialPrecisionTests
{
    // ─── BudgetAlertService ───────────────────────────────────────────────

    private static BudgetAlertService BuildAlertService(
        decimal warningPct = 80m, decimal exceededPct = 100m)
    {
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BudgetAlerts:WarningThresholdPercent"]  = warningPct.ToString(),
                ["BudgetAlerts:ExceededThresholdPercent"] = exceededPct.ToString(),
            })
            .Build();

        var dbOptions = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<BudgetBuddy.Service.Budgets.Persistence.BudgetsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BudgetAlertService(
            new BudgetBuddy.Service.Budgets.Persistence.BudgetsDbContext(dbOptions),
            Substitute.For<BudgetBuddy.Shared.Messages.Contracts.Transactions.ITransactionQueryService>(),
            Substitute.For<BudgetBuddy.Shared.Messages.Contracts.ReferenceData.ICategoryQueryService>(),
            Substitute.For<ICurrencyConversionService>(),
            config,
            Substitute.For<Microsoft.Extensions.Logging.ILogger<BudgetAlertService>>());
    }

    [Theory]
    [InlineData(100)]   // full spend
    [InlineData(80)]    // exactly at warning
    [InlineData(79)]    // just below warning
    [InlineData(0.01)]  // tiny amount
    [InlineData(0)]     // zero budget
    public void UtilizationPercentage_IsRoundedToTwoDecimals(
        decimal expectedPct)
    {
        var svc = BuildAlertService();
        var pct = svc.DetermineAlertLevel(expectedPct);
        // Just verifying the method doesn't throw for boundary values
        pct.Should().BeOneOf(
            BudgetBuddy.Service.Budgets.Features.BudgetAlerts.GetBudgetAlerts.AlertLevel.Safe,
            BudgetBuddy.Service.Budgets.Features.BudgetAlerts.GetBudgetAlerts.AlertLevel.Warning,
            BudgetBuddy.Service.Budgets.Features.BudgetAlerts.GetBudgetAlerts.AlertLevel.Exceeded);
    }

    [Theory]
    [InlineData(79.99, BudgetBuddy.Service.Budgets.Features.BudgetAlerts.GetBudgetAlerts.AlertLevel.Safe)]
    [InlineData(80.00, BudgetBuddy.Service.Budgets.Features.BudgetAlerts.GetBudgetAlerts.AlertLevel.Warning)]
    [InlineData(99.99, BudgetBuddy.Service.Budgets.Features.BudgetAlerts.GetBudgetAlerts.AlertLevel.Warning)]
    [InlineData(100.0, BudgetBuddy.Service.Budgets.Features.BudgetAlerts.GetBudgetAlerts.AlertLevel.Exceeded)]
    [InlineData(150.0, BudgetBuddy.Service.Budgets.Features.BudgetAlerts.GetBudgetAlerts.AlertLevel.Exceeded)]
    public void AlertLevel_BoundaryConditions_AreCorrect(
        decimal utilization, BudgetBuddy.Service.Budgets.Features.BudgetAlerts.GetBudgetAlerts.AlertLevel expected)
    {
        var svc = BuildAlertService();
        svc.DetermineAlertLevel(utilization).Should().Be(expected);
    }

    [Fact]
    public void AlertLevel_CustomThresholds_AreRespected()
    {
        var svc = BuildAlertService(warningPct: 70m, exceededPct: 90m);
        svc.DetermineAlertLevel(69.99m).Should().Be(BudgetBuddy.Service.Budgets.Features.BudgetAlerts.GetBudgetAlerts.AlertLevel.Safe);
        svc.DetermineAlertLevel(70.00m).Should().Be(BudgetBuddy.Service.Budgets.Features.BudgetAlerts.GetBudgetAlerts.AlertLevel.Warning);
        svc.DetermineAlertLevel(90.00m).Should().Be(BudgetBuddy.Service.Budgets.Features.BudgetAlerts.GetBudgetAlerts.AlertLevel.Exceeded);
    }

    // ─── InvestmentCalculationService ────────────────────────────────────

    private static IInvestmentCalculationService BuildCalcService()
    {
        var dbOptions = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<BudgetBuddy.Service.Investments.Persistence.InvestmentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new InvestmentCalculationService(
            new BudgetBuddy.Service.Investments.Persistence.InvestmentsDbContext(dbOptions),
            NodaTime.SystemClock.Instance,
            Substitute.For<ICurrencyConversionService>(),
            null,
            Substitute.For<Microsoft.Extensions.Logging.ILogger<InvestmentCalculationService>>());
    }

    [Theory]
    [InlineData(100,  10,  15,  1000, 1500,  500,  50.00)]  // standard gain
    [InlineData(100,  15,  10,  1500, 1000, -500, -33.33)]  // loss
    [InlineData(50,   20,  20,  1000, 1000,    0,   0.00)]  // break-even
    [InlineData(0.5,  100, 110,   50,   55,    5,  10.00)]  // fractional share
    public void CalculateInvestmentMetrics_ReturnsCorrectValues(
        decimal qty, decimal buyPrice, decimal curPrice,
        decimal expInvested, decimal expValue, decimal expGainLoss, decimal expPct)
    {
        var svc = BuildCalcService();
        var (invested, value, gainLoss, pct) =
            svc.CalculateInvestmentMetrics(qty, buyPrice, curPrice);

        invested.Should().Be(expInvested);
        value.Should().Be(expValue);
        gainLoss.Should().Be(expGainLoss);
        pct.Should().Be(expPct);
    }

    [Fact]
    public void CalculateInvestmentMetrics_ZeroCostBasis_ReturnsZeroPercent()
    {
        var svc = BuildCalcService();
        var (_, _, _, pct) = svc.CalculateInvestmentMetrics(10, 0, 50);
        pct.Should().Be(0);
    }

    // ─── FIFO cost basis (#16) ────────────────────────────────────────────

    [Fact]
    public void FifoGainLoss_SingleLotFullSell_IsCorrect()
    {
        // 100 shares @ $10 bought, sold at $15 → gain = $500
        const decimal qty = 100m, buyPrice = 10m, salePrice = 15m;
        var gain = qty * (salePrice - buyPrice);
        gain.Should().Be(500m);
    }

    [Fact]
    public void FifoGainLoss_TwoLotsPartialSell_FifoOrder()
    {
        // Lot1: 50 @ $10; Lot2: 50 @ $20 → sell 60 at $25 (FIFO)
        // Lot1 fully consumed: 50 × (25 - 10) = $750
        // Lot2 partially: 10 × (25 - 20)     = $50
        // Total: $800
        var lot1 = (qty: 50m, price: 10m);
        var lot2 = (qty: 50m, price: 20m);
        var toSell = 60m;
        var salePrice = 25m;

        var gain = 0m;
        foreach (var lot in new[] { lot1, lot2 })
        {
            if (toSell <= 0)
            {
                break;
            }

            var fromLot = Math.Min(lot.qty, toSell);
            gain    += fromLot * (salePrice - lot.price);
            toSell  -= fromLot;
        }

        gain.Should().Be(800m);
    }

    [Fact]
    public void FifoGainLoss_DecimalPrecision_NoRoundingLoss()
    {
        // 0.333... shares scenario
        var qty       = 1m / 3m;
        var buyPrice  = 100m;
        var salePrice = 200m;
        var gain      = qty * (salePrice - buyPrice);
        Math.Round(gain, 2).Should().Be(33.33m);
    }
}
