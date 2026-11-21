using CollegeErp.Application.Abstractions;
using CollegeErp.Application.Common;
using CollegeErp.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace CollegeErp.Application.Campus.Crud;

/// <summary>Generic create/read/update/delete over any entity in the current college's database.</summary>
public sealed class CrudService<TEntity, TWrite>(
    ICollegeDbContext db,
    IQueryFilter<TEntity> filter,
    IEnumerable<IEntityRule<TEntity>> rules,
    IEnumerable<IDeleteRule<TEntity>> deleteRules,
    TimeProvider clock) : ICrudService<TEntity, TWrite>
    where TEntity : Entity, new()
    where TWrite : IWriteModel<TEntity>
{
    private DbSet<TEntity> Set => db.Set<TEntity>();

    public async Task<PagedResult<TEntity>> ListAsync(ListQuery query, CancellationToken ct = default)
    {
        var q = filter.Apply(Set.AsNoTracking(), query);
        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(e => e.Id).Skip(query.SafeSkip).Take(query.SafeTake).ToListAsync(ct);
        return new PagedResult<TEntity>(items, total, query.SafeSkip, query.SafeTake);
    }

    public async Task<TEntity> GetAsync(int id, CancellationToken ct = default) =>
        await Set.FindAsync([id], ct) ?? throw NotFoundException.For<TEntity>(id);

    public async Task<TEntity> CreateAsync(TWrite model, CancellationToken ct = default)
    {
        var entity = new TEntity();
        model.ApplyTo(entity);
        if (entity is IAuditable auditable) auditable.CreatedAt = clock.GetUtcNow().UtcDateTime;
        await RunRulesAsync(entity, isNew: true, ct);
        Set.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<TEntity> UpdateAsync(int id, TWrite model, CancellationToken ct = default)
    {
        var entity = await GetAsync(id, ct);
        model.ApplyTo(entity);
        await RunRulesAsync(entity, isNew: false, ct);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await GetAsync(id, ct);
        foreach (var rule in deleteRules) await rule.ValidateDeleteAsync(entity, ct);
        Set.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    private async Task RunRulesAsync(TEntity entity, bool isNew, CancellationToken ct)
    {
        foreach (var rule in rules) await rule.ValidateAsync(entity, isNew, ct);
    }
}

/// <summary>Fallback filter for entities without custom search.</summary>
public sealed class NoFilter<TEntity> : IQueryFilter<TEntity> where TEntity : Entity
{
    public IQueryable<TEntity> Apply(IQueryable<TEntity> source, ListQuery query) => source;
}
