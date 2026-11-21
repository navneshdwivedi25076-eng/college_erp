using CollegeErp.Domain.Campus;
using CollegeErp.Domain.Platform;
using Microsoft.EntityFrameworkCore;

namespace CollegeErp.Application.Abstractions;

/// <summary>The platform database: super admins + the registry of colleges.</summary>
public interface IMasterDbContext
{
    DbSet<SuperAdmin> SuperAdmins { get; }
    DbSet<College> Colleges { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>ONE college's private database. Which database depends on the current tenant.</summary>
public interface ICollegeDbContext : IAsyncDisposable
{
    DbSet<User> Users { get; }
    DbSet<Department> Departments { get; }
    DbSet<FacultyMember> Faculty { get; }
    DbSet<Student> Students { get; }
    DbSet<Course> Courses { get; }
    DbSet<Enrollment> Enrollments { get; }
    DbSet<AttendanceRecord> Attendance { get; }
    DbSet<Exam> Exams { get; }
    DbSet<Mark> Marks { get; }
    DbSet<FeeInvoice> FeeInvoices { get; }
    DbSet<Payment> Payments { get; }
    DbSet<CollegeSetting> Settings { get; }
    DbSet<T> Set<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
