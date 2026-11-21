using System.ComponentModel.DataAnnotations;
using CollegeErp.Application.Abstractions;
using CollegeErp.Application.Common;
using CollegeErp.Domain.Campus;
using CollegeErp.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace CollegeErp.Application.Campus.Academics;

public sealed class AttendanceEntry
{
    [Required] public int StudentId { get; init; }
    [Required, RegularExpression("^(present|absent|late|excused)$")] public string Status { get; init; } = "";
}

public sealed class MarkAttendanceRequest
{
    [Required] public int CourseId { get; init; }
    [Required] public DateOnly Date { get; init; }
    [Required, MinLength(1)] public List<AttendanceEntry> Entries { get; init; } = [];
}

public sealed record AttendanceDto(int Id, int CourseId, int StudentId, DateOnly Date, string Status);

public sealed record AttendanceSummaryDto(
    int StudentId, string RollNo, string StudentName, int TotalClasses, int Attended, decimal Percentage);

public interface IAttendanceService
{
    Task<int> MarkAsync(MarkAttendanceRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceDto>> ListAsync(int? courseId, int? studentId, DateOnly? date, CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceSummaryDto>> SummaryAsync(int courseId, int? studentId = null, CancellationToken ct = default);
}

public sealed class AttendanceService(ICollegeDbContext db, ICourseAccessPolicy access, ICurrentUser user)
    : IAttendanceService
{
    /// <summary>Creates or updates attendance for each student on the given date (idempotent).</summary>
    public async Task<int> MarkAsync(MarkAttendanceRequest r, CancellationToken ct = default)
    {
        await access.EnsureCanManageAsync(r.CourseId, ct);
        var ids = r.Entries.Select(e => e.StudentId).Distinct().ToList();
        var enrolled = await db.Enrollments.Where(e => e.CourseId == r.CourseId && ids.Contains(e.StudentId))
            .Select(e => e.StudentId).Distinct().ToListAsync(ct);
        var notEnrolled = ids.Except(enrolled).ToList();
        if (notEnrolled.Count > 0)
            throw new InvalidInputException("entries", $"Students not enrolled in this course: {string.Join(", ", notEnrolled)}");

        var existing = await db.Attendance.Where(a => a.CourseId == r.CourseId && a.Date == r.Date && ids.Contains(a.StudentId))
            .ToDictionaryAsync(a => a.StudentId, ct);
        foreach (var entry in r.Entries)
        {
            if (existing.TryGetValue(entry.StudentId, out var rec))
            {
                rec.Status = entry.Status;
                rec.MarkedByUserId = user.UserId;
            }
            else
            {
                var record = new AttendanceRecord
                {
                    CourseId = r.CourseId, StudentId = entry.StudentId, Date = r.Date,
                    Status = entry.Status, MarkedByUserId = user.UserId,
                };
                db.Attendance.Add(record);
                existing[entry.StudentId] = record;
            }
        }
        await db.SaveChangesAsync(ct);
        return r.Entries.Count;
    }

    public async Task<IReadOnlyList<AttendanceDto>> ListAsync(
        int? courseId, int? studentId, DateOnly? date, CancellationToken ct = default)
    {
        var q = db.Attendance.AsNoTracking();
        if (courseId is { } c) q = q.Where(a => a.CourseId == c);
        if (studentId is { } s) q = q.Where(a => a.StudentId == s);
        if (date is { } d) q = q.Where(a => a.Date == d);
        return await q.OrderByDescending(a => a.Date).ThenBy(a => a.StudentId).Take(2000)
            .Select(a => new AttendanceDto(a.Id, a.CourseId, a.StudentId, a.Date, a.Status)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AttendanceSummaryDto>> SummaryAsync(
        int courseId, int? studentId = null, CancellationToken ct = default)
    {
        var q = from a in db.Attendance
                join s in db.Students on a.StudentId equals s.Id
                where a.CourseId == courseId
                select new { a, s };
        if (studentId is { } sid) q = q.Where(x => x.s.Id == sid);

        var rows = await q.GroupBy(x => new { x.s.Id, x.s.RollNo, x.s.FirstName, x.s.LastName })
            .Select(g => new
            {
                g.Key.Id, g.Key.RollNo, Name = g.Key.FirstName + " " + g.Key.LastName,
                Total = g.Count(),
                Attended = g.Count(x => x.a.Status == AttendanceStatus.Present || x.a.Status == AttendanceStatus.Late),
            }).ToListAsync(ct);

        return rows.OrderBy(r => r.RollNo).Select(r => new AttendanceSummaryDto(
            r.Id, r.RollNo, r.Name, r.Total, r.Attended,
            r.Total == 0 ? 0 : Math.Round(100m * r.Attended / r.Total, 1))).ToList();
    }
}
