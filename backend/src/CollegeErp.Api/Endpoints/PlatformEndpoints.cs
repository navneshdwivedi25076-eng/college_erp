using CollegeErp.Api.Infrastructure;
using CollegeErp.Application.Platform;
using CollegeErp.Domain.Common;

namespace CollegeErp.Api.Endpoints;

/// <summary>Super-admin API: onboard and manage colleges (tenants).</summary>
public static class PlatformEndpoints
{
    public static void MapPlatformEndpoints(this IEndpointRouteBuilder app)
    {
        var platform = app.MapGroup("/api/platform").WithTags("Platform");

        platform.MapPost("/auth/login", (LoginRequest r, IPlatformAuthService auth, CancellationToken ct) =>
            auth.LoginAsync(r, ct)).AllowAnonymous();

        var colleges = platform.MapGroup("/colleges").RequireAuthorization(Policies.Platform);

        colleges.MapGet("", (HttpRequest req, ICollegeAdministrationService svc, CancellationToken ct) =>
            svc.ListAsync(req.ToListQuery(), ct));

        colleges.MapPost("", async (CreateCollegeRequest r, ICollegeOnboardingService svc, CancellationToken ct) =>
        {
            var college = await svc.CreateCollegeAsync(r, ct);
            return Results.Created($"/api/platform/colleges/{college.Id}", college);
        });

        colleges.MapGet("/{id:int}", (int id, ICollegeAdministrationService svc, CancellationToken ct) =>
            svc.GetAsync(id, ct));

        colleges.MapPut("/{id:int}", (int id, UpdateCollegeRequest r, ICollegeAdministrationService svc, CancellationToken ct) =>
            svc.UpdateAsync(id, r, ct));

        colleges.MapPost("/{id:int}/suspend", (int id, ICollegeAdministrationService svc, CancellationToken ct) =>
            svc.SetStatusAsync(id, CollegeStatus.Suspended, ct));

        colleges.MapPost("/{id:int}/activate", (int id, ICollegeAdministrationService svc, CancellationToken ct) =>
            svc.SetStatusAsync(id, CollegeStatus.Active, ct));

        colleges.MapDelete("/{id:int}", async (int id, bool? dropDatabase, ICollegeAdministrationService svc, CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, dropDatabase ?? false, ct);
            return Results.NoContent();
        });

        colleges.MapGet("/{id:int}/stats", (int id, ICollegeAdministrationService svc, CancellationToken ct) =>
            svc.GetStatsAsync(id, ct));

        colleges.MapPost("/migrate", (ICollegeAdministrationService svc, CancellationToken ct) => svc.MigrateAllAsync(ct));

        var admins = platform.MapGroup("/admins").RequireAuthorization(Policies.Platform);
        admins.MapGet("", (ISuperAdminService svc, CancellationToken ct) => svc.ListAsync(ct));
        admins.MapPost("", async (CreateSuperAdminRequest r, ISuperAdminService svc, CancellationToken ct) =>
            Results.Created("/api/platform/admins", await svc.CreateAsync(r, ct)));
    }
}
