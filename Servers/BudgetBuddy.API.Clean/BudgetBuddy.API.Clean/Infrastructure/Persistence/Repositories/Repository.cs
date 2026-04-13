using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Infrastructure.Persistence.Repositories;

public abstract class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext Context;

    protected Repository(AppDbContext context)
    {
        Context = context;
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await Context.Set<T>().FindAsync([id], ct);

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
        => await Context.Set<T>().AddAsync(entity, ct);

    public virtual void Add(T entity)
        => Context.Set<T>().Add(entity);

    public virtual void Remove(T entity)
        => Context.Set<T>().Remove(entity);

    public virtual void RemoveRange(IEnumerable<T> entities)
        => Context.Set<T>().RemoveRange(entities);
}
