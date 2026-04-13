using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository(AppDbContext context) : Repository<RefreshToken>(context), IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
    {
        return await Context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == tokenHash, ct);
    }

    public async Task<List<RefreshToken>> GetActiveByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return await Context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct);
    }
}
