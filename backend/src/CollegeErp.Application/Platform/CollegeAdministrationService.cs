using CollegeErp.Application.Abstractions;
using CollegeErp.Application.Common;
using CollegeErp.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CollegeEntity = CollegeErp.Domain.Platform.College;

namespace CollegeErp.Application.Platform;

public interface ICollegeAdministrationService
{
    Task<PagedResult<CollegeDto>> ListAsync(ListQuery query, CancellationToken ct = default);
    Task<CollegeDto> GetAsync(int id, CancellationToken ct = default);
    Task<CollegeDto> UpdateAsync(int id, UpdateCollegeRequest request, CancellationToken ct = default);
    Task<CollegeDto> SetStatusAsync(int id, string status, CancellationToken ct = default);
    Task DeleteAsync(int id, bool dropDatabase, CancellationToken ct = default);
    Task<CollegeStatsDto> GetStatsAsync(int id, CancellationToken ct = default);
    Task<MigrationReport> MigrateAllAsync(CancellationToken ct = default);
}

public sealed class CollegeAdministrationService(
    IMasterDbContext master,
    ITenantDatabaseManager databases,
    ICollegeDbContextFactory collegeContexts,
    ICollegeDirectory directory,
    ILogger<CollegeAdministrationService> logger) : ICollegeAdministrationService
{
    public async Task<PagedResult<CollegeDto>> ListAsync(ListQuery query, CancellationToken ct = default)
    {
        var q = master.Colleges.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(c => c.Name.ToLower().Contains(s) || c.Code.Contains(s));
        }
        if (query.StringFilter("status") is { } status) q = q.Where(c => c.Status == status);

        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(c => c.Name).Skip(query.SafeSkip).Take(query.SafeTake).ToListAsync(ct);
        return new PagedResult<CollegeDto>(items.Select(c => c.ToDto()).ToList(), total, query.SafeSkip, query.SafeTake);
    }

    public async Task<CollegeDto> GetAsync(int id, CancellationToken ct = default) =>
        (await FindAsync(id, ct)).ToDto();

    public async Task<CollegeDto> UpdateAsync(int id, UpdateCollegeRequest request, CancellationToken ct = default)
    {
        var college = await FindAsync(id, ct);
        college.Name = request.Name.Trim();
        college.Address = request.Address;
        college.ContactEmail = request.ContactEmail;
        college.ContactPhone = request.ContactPhone;
        college.Plan = request.Plan;
        college.MaxStudents = request.MaxStudents;
        await master.SaveChangesAsync(ct);
        directory.Invalidate(college.Code);
        return college.ToDto();
    }

    public async Task<CollegeDto> SetStatusAsync(int id, string status, CancellationToken ct = default)
    {
        if (status is not (CollegeStatus.Active or CollegeStatus.Suspended))
            throw new InvalidInputException("status", "Status must be 'active' or 'suspended'.");
        var college = await FindAsync(id, ct);
        college.Status = status;
        await master.SaveChangesAsync(ct);
        directory.Invalidate(college.Code);
        return college.ToDto();
    }

    public async Task DeleteAsync(int id, bool dropDatabase, CancellationToken ct = default)
    {
        var college = await FindAsync(id, ct);
        master.Colleges.Remove(college);
        await master.SaveChangesAsync(ct);
        directory.Invalidate(college.Code);
        if (dropDatabase)
        {
            await databases.DropAsync(college.DatabaseName, ct);
            logger.LogWarning("Dropped database {Db} of college {Code}", college.DatabaseName, college.Code);
        }
    }

    public async Task<CollegeStatsDto> GetStatsAsync(int id, CancellationToken ct = default)
    {
        var college = await FindAsync(id, ct);
        await using var db = collegeContexts.Create(college.DatabaseName);
        return new CollegeStatsDto(
            college.Id,
            await db.Students.CountAsync(ct),
            await db.Faculty.CountAsync(ct),
            await db.Courses.CountAsync(ct),
            await db.Departments.CountAsync(ct),
            await db.Users.CountAsync(ct));
    }

    public async Task<MigrationReport> MigrateAllAsync(CancellationToken ct = default)
    {
        var colleges = await master.Colleges.ToListAsync(ct);
        var failures = new List<string>();
        var upgraded = 0;
        foreach (var college in colleges)
        {
            try
            {
                college.SchemaVersion = await databases.MigrateAsync(college.DatabaseName, ct);
                upgraded++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Migration failed for college {Code}", college.Code);
                failures.Add($"{college.Code}: {ex.Message}");
            }
        }
        await master.SaveChangesAsync(ct);
        return new MigrationReport(upgraded, failures);
    }

    private async Task<CollegeEntity> FindAsync(int id, CancellationToken ct) =>
        await master.Colleges.FindAsync([id], ct) ?? throw NotFoundException.For<CollegeEntity>(id);
}
