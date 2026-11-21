using CollegeErp.Application.Campus.Crud;
using CollegeErp.Application.Common;
using CollegeErp.Domain.Campus;

namespace CollegeErp.Application.Campus.Academics;

public sealed class DepartmentFilter : IQueryFilter<Department>
{
    public IQueryable<Department> Apply(IQueryable<Department> q, ListQuery query)
    {
        if (query.Search is { Length: > 0 } s)
        {
            s = s.ToLower();
            q = q.Where(d => d.Name.ToLower().Contains(s) || d.Code.ToLower().Contains(s));
        }
        return q;
    }
}

public sealed class FacultyFilter : IQueryFilter<FacultyMember>
{
    public IQueryable<FacultyMember> Apply(IQueryable<FacultyMember> q, ListQuery query)
    {
        if (query.Search is { Length: > 0 } s)
        {
            s = s.ToLower();
            q = q.Where(f => f.FirstName.ToLower().Contains(s) || f.LastName.ToLower().Contains(s)
                             || f.EmployeeNo.ToLower().Contains(s));
        }
        if (query.IntFilter("departmentId") is { } dep) q = q.Where(f => f.DepartmentId == dep);
        return q;
    }
}

public sealed class StudentFilter : IQueryFilter<Student>
{
    public IQueryable<Student> Apply(IQueryable<Student> q, ListQuery query)
    {
        if (query.Search is { Length: > 0 } s)
        {
            s = s.ToLower();
            q = q.Where(x => x.FirstName.ToLower().Contains(s) || x.LastName.ToLower().Contains(s)
                             || x.RollNo.ToLower().Contains(s));
        }
        if (query.IntFilter("departmentId") is { } dep) q = q.Where(x => x.DepartmentId == dep);
        if (query.IntFilter("semester") is { } sem) q = q.Where(x => x.CurrentSemester == sem);
        if (query.StringFilter("status") is { } status) q = q.Where(x => x.Status == status);
        return q;
    }
}

public sealed class CourseFilter : IQueryFilter<Course>
{
    public IQueryable<Course> Apply(IQueryable<Course> q, ListQuery query)
    {
        if (query.Search is { Length: > 0 } s)
        {
            s = s.ToLower();
            q = q.Where(c => c.Title.ToLower().Contains(s) || c.Code.ToLower().Contains(s));
        }
        if (query.IntFilter("departmentId") is { } dep) q = q.Where(c => c.DepartmentId == dep);
        if (query.IntFilter("facultyId") is { } fac) q = q.Where(c => c.FacultyId == fac);
        if (query.IntFilter("semester") is { } sem) q = q.Where(c => c.Semester == sem);
        return q;
    }
}

public sealed class ExamFilter : IQueryFilter<Exam>
{
    public IQueryable<Exam> Apply(IQueryable<Exam> q, ListQuery query)
    {
        if (query.IntFilter("courseId") is { } course) q = q.Where(e => e.CourseId == course);
        return q;
    }
}
