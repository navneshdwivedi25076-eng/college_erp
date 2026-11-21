using System.ComponentModel.DataAnnotations;
using CollegeErp.Application.Abstractions;
using CollegeErp.Application.Common;
using CollegeErp.Domain.Campus;
using Microsoft.EntityFrameworkCore;

namespace CollegeErp.Application.Campus.Academics;

public sealed class EnrollRequest
{
    [Required] public int CourseId { get; init; }
    [Required, MinLength(1)] public List<int> StudentIds { get; init; } = [];
    [Required, StringLength(20)] public string AcademicYear { get; init; } = "";
}

public sealed record EnrollmentDto(
    int Id, int StudentId, string RollNo, string StudentName, int CourseId, string CourseCode, string AcademicYear);

public interface IEnrollmentService
{
    Task<IReadOnlyList<EnrollmentDto>> ListAsync(int? courseId, int? studentId, string? academicYear, CancellationToken ct = default);
    Task<int> EnrollAsync(EnrollRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class EnrollmentService(ICollegeDbContext db, TimeProvider clock) : IEnrollmentService
{
    public async Task<IReadOnlyList<EnrollmentDto>> ListAsync(
        int? courseId, int? studentId, string? academicYear, CancellationToken ct = default)
    {
        var q = from e in db.Enrollments
                join s in db.Students on e.StudentId equals s.Id
                join c in db.Courses on e.CourseId equals c.Id
                select new { e, s, c };
        if (courseId is { } cid) q = q.Where(x => x.e.CourseId == cid);
        if (studentId is { } sid) q = q.Where(x => x.e.StudentId == sid);
        if (!string.IsNullOrWhiteSpace(academicYear)) q = q.Where(x => x.e.AcademicYear == academicYear);

        return await q.OrderBy(x => x.s.RollNo)
            .Select(x => new EnrollmentDto(x.e.Id, x.s.Id, x.s.RollNo, x.s.FirstName + " " + x.s.LastName,
                x.c.Id, x.c.Code, x.e.AcademicYear))
            .ToListAsync(ct);
    }

    /// <summary>Enrolls students into a course; already-enrolled students are skipped. Returns how many were added.</summary>
    public async Task<int> EnrollAsync(EnrollRequest r, CancellationToken ct = default)
    {
        if (!await db.Courses.AnyAsync(c => c.Id == r.CourseId, ct)) throw NotFoundException.For<Course>(r.CourseId);
        var ids = r.StudentIds.Distinct().ToList();
        var existingStudents = await db.Students.Where(s => ids.Contains(s.Id)).Select(s => s.Id).ToListAsync(ct);
        var missing = ids.Except(existingStudents).ToList();
        if (missing.Count > 0) throw new InvalidInputException("studentIds", $"Unknown students: {string.Join(", ", missing)}");

        var already = await db.Enrollments
            .Where(e => e.CourseId == r.CourseId && e.AcademicYear == r.AcademicYear && ids.Contains(e.StudentId))
            .Select(e => e.StudentId).ToListAsync(ct);
        var now = clock.GetUtcNow().UtcDateTime;
        var toAdd = ids.Except(already).Select(sid => new Enrollment
        {
            CourseId = r.CourseId, StudentId = sid, AcademicYear = r.AcademicYear, EnrolledOn = now,
        }).ToList();
        db.Enrollments.AddRange(toAdd);
        await db.SaveChangesAsync(ct);
        return toAdd.Count;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var e = await db.Enrollments.FindAsync([id], ct) ?? throw NotFoundException.For<Enrollment>(id);
        db.Enrollments.Remove(e);
        await db.SaveChangesAsync(ct);
    }
}
