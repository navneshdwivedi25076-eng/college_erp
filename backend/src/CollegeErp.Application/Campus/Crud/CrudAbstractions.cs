using CollegeErp.Application.Common;
using CollegeErp.Domain.Common;

namespace CollegeErp.Application.Campus.Crud;

/// <summary>A request body that knows how to copy its values onto an entity.</summary>
public interface IWriteModel<in TEntity> where TEntity : Entity
{
    void ApplyTo(TEntity entity);
}

/// <summary>
/// Business rule executed before an entity is saved (Open/Closed: add a rule by
/// registering a new class, without touching <see cref="CrudService{TEntity,TWrite}"/>).
/// </summary>
public interface IEntityRule<in TEntity> where TEntity : Entity
{
    Task ValidateAsync(TEntity entity, bool isNew, CancellationToken ct);
}

/// <summary>Rule executed before an entity is deleted.</summary>
public interface IDeleteRule<in TEntity> where TEntity : Entity
{
    Task ValidateDeleteAsync(TEntity entity, CancellationToken ct);
}

/// <summary>Applies search text and filters for an entity's list endpoint.</summary>
public interface IQueryFilter<TEntity> where TEntity : Entity
{
    IQueryable<TEntity> Apply(IQueryable<TEntity> source, ListQuery query);
}

public interface ICrudService<TEntity, in TWrite>
    where TEntity : Entity
    where TWrite : IWriteModel<TEntity>
{
    Task<PagedResult<TEntity>> ListAsync(ListQuery query, CancellationToken ct = default);
    Task<TEntity> GetAsync(int id, CancellationToken ct = default);
    Task<TEntity> CreateAsync(TWrite model, CancellationToken ct = default);
    Task<TEntity> UpdateAsync(int id, TWrite model, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
