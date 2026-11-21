using CollegeErp.Application.Abstractions;
using CollegeErp.Application.Campus.Crud;
using CollegeErp.Application.Common;
using CollegeErp.Domain.Campus;
using Microsoft.EntityFrameworkCore;

namespace CollegeErp.Application.Campus.Academics;

/// <summary>Enforces the college's plan limit on the number of students.</summary>
public sealed class StudentCapacityRule(ICollegeDbContext db, ICurrentTenant tenant) : IEntityRule<Student>
{
    public async Task ValidateAsync(Student entity, bool isNew, CancellationToken ct)
    {
        if (!isNew || tenant.College.MaxStudents is not { } max) return;
        if (await db.Students.CountAsync(ct) >= max)
            throw new ForbiddenException($"Your plan allows at most {max} students. Contact the platform admin to upgrade.");
    }
}

public sealed class StudentReferencesRule(ReferenceGuard guard) : IEntityRule<Student>
{
    public Task ValidateAsync(Student e, bool isNew, CancellationToken ct) =>
        guard.EnsureExistsAsync<Department>(e.DepartmentId, nameof(e.DepartmentId), ct);
}

public sealed class FacultyReferencesRule(ReferenceGuard guard) : IEntityRule<FacultyMember>
{
    public Task ValidateAsync(FacultyMember e, bool isNew, CancellationToken ct) =>
        guard.EnsureExistsAsync<Department>(e.DepartmentId, nameof(e.DepartmentId), ct);
}

public sealed class CourseReferencesRule(ReferenceGuard guard) : IEntityRule<Course>
{
    public async Task ValidateAsync(Course e, bool isNew, CancellationToken ct)
    {
        await guard.EnsureExistsAsync<Department>(e.DepartmentId, nameof(e.DepartmentId), ct);
        await guard.EnsureExistsAsync<FacultyMember>(e.FacultyId, nameof(e.FacultyId), ct);
    }
}

/// <summary>Exams can only be created/edited for a course the current user manages.</summary>
public sealed class ExamCourseAccessRule(ICourseAccessPolicy access) : IEntityRule<Exam>
{
    public Task ValidateAsync(Exam e, bool isNew, CancellationToken ct) => access.EnsureCanManageAsync(e.CourseId, ct);
}

public sealed class ExamDeleteAccessRule(ICourseAccessPolicy access) : IDeleteRule<Exam>
{
    public Task ValidateDeleteAsync(Exam e, CancellationToken ct) => access.EnsureCanManageAsync(e.CourseId, ct);
}

/// <summary>Prevents deleting a department that still has students, staff or courses.</summary>
public sealed class DepartmentInUseRule(ICollegeDbContext db) : IDeleteRule<Department>
{
    public async Task ValidateDeleteAsync(Department d, CancellationToken ct)
    {
        if (await db.Students.AnyAsync(s => s.DepartmentId == d.Id, ct)
            || await db.Faculty.AnyAsync(f => f.DepartmentId == d.Id, ct)
            || await db.Courses.AnyAsync(c => c.DepartmentId == d.Id, ct))
            throw new ConflictException("Department is in use by students, faculty or courses.");
    }
}
