using CollegeErp.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CollegeErp.Application.Campus;

public sealed record DashboardDto(
    string CollegeName, string Plan, int? MaxStudents,
    int Students, int ActiveStudents, int Faculty, int Departments, int Courses, int Users,
    decimal FeesBilled, decimal FeesCollected, decimal FeesOutstanding);

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(CancellationToken ct = default);
}

public sealed class DashboardService(ICollegeDbContext db, ICurrentTenant tenant) : IDashboardService
{
    public async Task<DashboardDto> GetAsync(CancellationToken ct = default)
    {
        var billed = await db.FeeInvoices.SumAsync(i => (decimal?)i.Amount, ct) ?? 0;
        var collected = await db.FeeInvoices.SumAsync(i => (decimal?)i.AmountPaid, ct) ?? 0;
        return new DashboardDto(
            tenant.College.Name, tenant.College.Plan, tenant.College.MaxStudents,
            await db.Students.CountAsync(ct),
            await db.Students.CountAsync(s => s.Status == "active", ct),
            await db.Faculty.CountAsync(ct),
            await db.Departments.CountAsync(ct),
            await db.Courses.CountAsync(ct),
            await db.Users.CountAsync(ct),
            billed, collected, billed - collected);
    }
}
