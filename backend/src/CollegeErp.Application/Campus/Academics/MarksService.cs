using System.ComponentModel.DataAnnotations;
using CollegeErp.Application.Abstractions;
using CollegeErp.Application.Common;
using CollegeErp.Domain.Campus;
using Microsoft.EntityFrameworkCore;

namespace CollegeErp.Application.Campus.Academics;

public sealed class MarkEntry
{
    [Required] public int StudentId { get; init; }
    [Range(0, 10000)] public decimal MarksObtained { get; init; }
    [StringLength(255)] public string? Remarks { get; init; }
}

public sealed class RecordMarksRequest
{
    [Required, MinLength(1)] public List<MarkEntry> Entries { get; init; } = [];
}

public sealed record MarkDto(
    int Id, int ExamId, string ExamName, int CourseId, int StudentId, string RollNo, string StudentName,
    decimal MarksObtained, decimal MaxMarks, decimal Percentage, string? Remarks);

public interface IMarksService
{
    Task<int> RecordAsync(int examId, RecordMarksRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<MarkDto>> ListAsync(int? examId, int? studentId, CancellationToken ct = default);
}

public sealed class MarksService(ICollegeDbContext db, ICourseAccessPolicy access) : IMarksService
{
    public async Task<int> RecordAsync(int examId, RecordMarksRequest r, CancellationToken ct = default)
    {
        var exam = await db.Exams.FindAsync([examId], ct) ?? throw NotFoundException.For<Exam>(examId);
        await access.EnsureCanManageAsync(exam.CourseId, ct);

        var over = r.Entries.Where(e => e.MarksObtained > exam.MaxMarks).Select(e => e.StudentId).ToList();
        if (over.Count > 0)
            throw new InvalidInputException("entries", $"Marks exceed maximum ({exam.MaxMarks}) for students: {string.Join(", ", over)}");

        var ids = r.Entries.Select(e => e.StudentId).Distinct().ToList();
        var enrolled = await db.Enrollments.Where(e => e.CourseId == exam.CourseId && ids.Contains(e.StudentId))
            .Select(e => e.StudentId).Distinct().ToListAsync(ct);
        var notEnrolled = ids.Except(enrolled).ToList();
        if (notEnrolled.Count > 0)
            throw new InvalidInputException("entries", $"Students not enrolled in this course: {string.Join(", ", notEnrolled)}");

        var existing = await db.Marks.Where(m => m.ExamId == examId && ids.Contains(m.StudentId))
            .ToDictionaryAsync(m => m.StudentId, ct);
        foreach (var e in r.Entries)
        {
            if (existing.TryGetValue(e.StudentId, out var mark))
            {
                mark.MarksObtained = e.MarksObtained;
                mark.Remarks = e.Remarks;
            }
            else
            {
                var m = new Mark { ExamId = examId, StudentId = e.StudentId, MarksObtained = e.MarksObtained, Remarks = e.Remarks };
                db.Marks.Add(m);
                existing[e.StudentId] = m;
            }
        }
        await db.SaveChangesAsync(ct);
        return r.Entries.Count;
    }

    public async Task<IReadOnlyList<MarkDto>> ListAsync(int? examId, int? studentId, CancellationToken ct = default)
    {
        var q = from m in db.Marks
                join e in db.Exams on m.ExamId equals e.Id
                join s in db.Students on m.StudentId equals s.Id
                select new { m, e, s };
        if (examId is { } eid) q = q.Where(x => x.e.Id == eid);
        if (studentId is { } sid) q = q.Where(x => x.s.Id == sid);

        var rows = await q.Select(x => new
        {
            x.m.Id, ExamId = x.e.Id, ExamName = x.e.Name, x.e.CourseId, StudentId = x.s.Id, x.s.RollNo,
            Name = x.s.FirstName + " " + x.s.LastName, x.m.MarksObtained, x.e.MaxMarks, x.m.Remarks,
        }).ToListAsync(ct);

        return rows.OrderBy(r => r.ExamId).ThenBy(r => r.RollNo).Select(r => new MarkDto(
            r.Id, r.ExamId, r.ExamName, r.CourseId, r.StudentId, r.RollNo, r.Name, r.MarksObtained, r.MaxMarks,
            r.MaxMarks == 0 ? 0 : Math.Round(100m * r.MarksObtained / r.MaxMarks, 1), r.Remarks)).ToList();
    }
}
