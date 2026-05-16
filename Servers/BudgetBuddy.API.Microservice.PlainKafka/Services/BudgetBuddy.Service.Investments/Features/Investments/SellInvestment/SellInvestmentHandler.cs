using BudgetBuddy.Service.Investments.Domain;
using BudgetBuddy.Service.Investments.Persistence;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Infrastructure.Services;
using BudgetBuddy.Shared.Kernel.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.Service.Investments.Features.Investments.SellInvestment;

/// <summary>
/// Allocates a sell order against existing purchase lots using FIFO (First In, First Out):
/// the oldest lots are consumed first, matching the most common tax treatment.
///
/// Each lot's <see cref="Investment.SoldQuantity"/> is incremented and
/// <see cref="Investment.SalePrice"/> is recorded. When a lot is fully consumed,
/// <see cref="Investment.SoldDate"/> is set. The realized gain/loss is the sum of
/// <c>(salePrice − purchasePrice) × quantitySold</c> across all affected lots.
/// </summary>
public class SellInvestmentHandler(
    InvestmentsDbContext context,
    ICurrentUserService currentUserService,
    IUserCacheInvalidator cacheInvalidator,
    ILogger<SellInvestmentHandler> logger)
    : UserAwareHandler<SellInvestmentCommand, SellInvestmentResponse>(currentUserService)
{
    public override async Task<SellInvestmentResponse> Handle(
        SellInvestmentCommand request,
        CancellationToken cancellationToken)
    {
        // FIFO: oldest lots first
        var lots = await context.Investments
            .Where(i => i.UserId == UserId &&
                        i.Symbol == request.Symbol &&
                        (i.SoldDate == null || (i.SoldQuantity ?? 0) < i.Quantity))
            .OrderBy(i => i.PurchaseDate)
            .ThenBy(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

        if (lots.Count == 0)
        {
            throw new NotFoundException("Investment", request.Symbol);
        }

        var totalAvailable = lots.Sum(l => l.Quantity - (l.SoldQuantity ?? 0));
        if (request.QuantityToSell > totalAvailable)
        {
            throw new InvalidOperationException(
                $"Cannot sell {request.QuantityToSell} of '{request.Symbol}': " +
                $"only {totalAvailable} units available.");
        }

        var remaining = request.QuantityToSell;
        var realizedGainLoss = 0m;

        foreach (var lot in lots)
        {
            if (remaining <= 0)
            {
                break;
            }

            var lotAvailable  = lot.Quantity - (lot.SoldQuantity ?? 0);
            var sellFromLot   = Math.Min(lotAvailable, remaining);
            var lotGainLoss   = sellFromLot * (request.SalePrice - lot.PurchasePrice);

            realizedGainLoss  += lotGainLoss;
            lot.SoldQuantity   = (lot.SoldQuantity ?? 0) + sellFromLot;
            lot.SalePrice      = request.SalePrice;

            if (lot.SoldQuantity >= lot.Quantity)
            {
                lot.SoldDate = request.SaleDate;
            }

            remaining -= sellFromLot;

            logger.LogDebug(
                "FIFO: sold {SellFromLot} of lot {LotId} ({Symbol}, purchased {Date}) — " +
                "gain/loss on lot: {LotGainLoss}",
                sellFromLot, lot.Id, lot.Symbol, lot.PurchaseDate, lotGainLoss);
        }

        await context.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        logger.LogInformation(
            "Sold {Qty} × {Symbol} at {Price} — realized gain/loss: {GainLoss}",
            request.QuantityToSell, request.Symbol, request.SalePrice,
            Math.Round(realizedGainLoss, 2));

        return new SellInvestmentResponse(
            request.Symbol,
            request.QuantityToSell,
            request.SalePrice,
            Math.Round(realizedGainLoss, 2));
    }
}
