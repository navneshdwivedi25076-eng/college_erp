using CollegeErp.Application.Abstractions;
using CollegeErp.Application.Common;
using CollegeErp.Domain.Campus;
using CollegeErp.Domain.Common;

namespace CollegeErp.Application.Campus.Academics;

public interface ICourseAccessPolicy
{
    /// <summary>Throws unless the current user may manage (attendance/marks of) this course.</summary>
    Task<Course> EnsureCanManageAsync(int courseId, CancellationToken ct);
}

/// <summary>Admins manage every course; faculty only the courses assigned to them.</summary>
public sealed class CourseAccessPolicy(ICollegeDbContext db, ICurrentUser user) : ICourseAccessPolicy
{
    public async Task<Course> EnsureCanManageAsync(int courseId, CancellationToken ct)
    {
        var course = await db.Courses.FindAsync([courseId], ct) ?? throw NotFoundException.For<Course>(courseId);
        if (user.Role == Roles.CollegeAdmin) return course;
        if (user.Role == Roles.Faculty && user.FacultyId is { } fid && course.FacultyId == fid) return course;
        throw new ForbiddenException("You are not assigned to this course.");
    }
}
