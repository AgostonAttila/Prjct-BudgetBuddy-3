using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Infrastructure.Persistence.Repositories;

public abstract class UserOwnedRepository<T> : Repository<T>, IUserOwnedRepository<T>
    where T : class, IUserOwnedEntity
{
    protected UserOwnedRepository(AppDbContext context) : base(context) { }

    public virtual async Task<T?> GetByIdAsync(Guid id, string userId, CancellationToken ct = default)
        => await Context.Set<T>().FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, ct);

    public virtual async Task<List<T>> GetByUserAsync(string userId, CancellationToken ct = default)
        => await Context.Set<T>().Where(e => e.UserId == userId).ToListAsync(ct);
}
