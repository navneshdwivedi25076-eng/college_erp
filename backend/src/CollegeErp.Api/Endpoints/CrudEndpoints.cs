using CollegeErp.Api.Infrastructure;
using CollegeErp.Application.Campus.Crud;
using CollegeErp.Domain.Common;

namespace CollegeErp.Api.Endpoints;

public static class CrudEndpoints
{
    /// <summary>Maps list/get/create/update/delete for one entity in a single line.</summary>
    public static RouteGroupBuilder MapCrud<TEntity, TWrite>(
        this RouteGroupBuilder parent, string path, string readPolicy, string writePolicy)
        where TEntity : Entity
        where TWrite : IWriteModel<TEntity>
    {
        var g = parent.MapGroup(path).WithTags(path.Trim('/'));

        g.MapGet("", (HttpRequest req, ICrudService<TEntity, TWrite> svc, CancellationToken ct) =>
            svc.ListAsync(req.ToListQuery(), ct)).RequireAuthorization(readPolicy);

        g.MapGet("/{id:int}", (int id, ICrudService<TEntity, TWrite> svc, CancellationToken ct) =>
            svc.GetAsync(id, ct)).RequireAuthorization(readPolicy);

        g.MapPost("", async (TWrite model, HttpRequest req, ICrudService<TEntity, TWrite> svc, CancellationToken ct) =>
        {
            var entity = await svc.CreateAsync(model, ct);
            return Results.Created($"{req.Path}/{entity.Id}", entity);
        }).RequireAuthorization(writePolicy);

        g.MapPut("/{id:int}", (int id, TWrite model, ICrudService<TEntity, TWrite> svc, CancellationToken ct) =>
            svc.UpdateAsync(id, model, ct)).RequireAuthorization(writePolicy);

        g.MapDelete("/{id:int}", async (int id, ICrudService<TEntity, TWrite> svc, CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        }).RequireAuthorization(writePolicy);

        return g;
    }
}
