using CollegeErp.Application.Abstractions;
using CollegeErp.Application.Campus.Academics;
using CollegeErp.Application.Campus.Finance;
using CollegeErp.Application.Common;
using CollegeErp.Domain.Campus;
using Microsoft.EntityFrameworkCore;

namespace CollegeErp.Application.Campus.Portal;

public sealed record MyCourseDto(int CourseId, string Code, string Title, decimal Credits, string AcademicYear,
    int TotalClasses, int Attended, decimal AttendancePercentage);

public interface IStudentPortalService
{
    Task<Student> ProfileAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MyCourseDto>> CoursesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MarkDto>> MarksAsync(CancellationToken ct = default);
    Task<IReadOnlyList<InvoiceDto>> InvoicesAsync(CancellationToken ct = default);
}

/// <summary>Read-only views of a logged-in student's own data.</summary>
public sealed class StudentPortalService(
    ICollegeDbContext db, ICurrentUser user, IMarksService marks, IAttendanceService attendance) : IStudentPortalService
{
    private int StudentId => user.StudentId ?? throw new ForbiddenException("Your account is not linked to a student record.");

    public async Task<Student> ProfileAsync(CancellationToken ct = default) =>
        await db.Students.FindAsync([StudentId], ct) ?? throw NotFoundException.For<Student>(StudentId);

    public async Task<IReadOnlyList<MyCourseDto>> CoursesAsync(CancellationToken ct = default)
    {
        var sid = StudentId;
        var courses = await (from e in db.Enrollments
                             join c in db.Courses on e.CourseId equals c.Id
                             where e.StudentId == sid
                             select new { c.Id, c.Code, c.Title, c.Credits, e.AcademicYear }).ToListAsync(ct);
        var result = new List<MyCourseDto>();
        foreach (var c in courses)
        {
            var summary = (await attendance.SummaryAsync(c.Id, sid, ct)).FirstOrDefault();
            result.Add(new MyCourseDto(c.Id, c.Code, c.Title, c.Credits, c.AcademicYear,
                summary?.TotalClasses ?? 0, summary?.Attended ?? 0, summary?.Percentage ?? 0));
        }
        return result;
    }

    public Task<IReadOnlyList<MarkDto>> MarksAsync(CancellationToken ct = default) => marks.ListAsync(null, StudentId, ct);

    public async Task<IReadOnlyList<InvoiceDto>> InvoicesAsync(CancellationToken ct = default)
    {
        var student = await ProfileAsync(ct);
        var invoices = await db.FeeInvoices.AsNoTracking().Where(i => i.StudentId == student.Id)
            .OrderByDescending(i => i.Id).ToListAsync(ct);
        return invoices.Select(i => FeeService.ToDto(i, student)).ToList();
    }
}
