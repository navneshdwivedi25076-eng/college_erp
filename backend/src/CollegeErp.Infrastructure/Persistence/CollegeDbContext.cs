using CollegeErp.Application.Abstractions;
using CollegeErp.Domain.Campus;
using Microsoft.EntityFrameworkCore;

namespace CollegeErp.Infrastructure.Persistence;

/// <summary>
/// EF Core context for ONE college's database. The connection string is chosen per
/// request from the resolved tenant, so the same model serves every college.
/// </summary>
public sealed class CollegeDbContext(DbContextOptions<CollegeDbContext> options) : DbContext(options), ICollegeDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<FacultyMember> Faculty => Set<FacultyMember>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<AttendanceRecord> Attendance => Set<AttendanceRecord>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<Mark> Marks => Set<Mark>();
    public DbSet<FeeInvoice> FeeInvoices => Set<FeeInvoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CollegeSetting> Settings => Set<CollegeSetting>();
    public DbSet<SchemaVersionEntry> SchemaVersions => Set<SchemaVersionEntry>();

    protected override void ConfigureConventions(ModelConfigurationBuilder c)
    {
        // SQLite has no native decimal; store as REAL so SUM/ORDER BY run in the database.
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            c.Properties<decimal>().HaveConversion<double>();
        else
            c.Properties<decimal>().HavePrecision(18, 2);
        c.Properties<string>().HaveMaxLength(255);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<SchemaVersionEntry>(e => { e.ToTable("schema_versions"); e.HasKey(x => x.Version); });

        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne<Student>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne<FacultyMember>().WithMany().HasForeignKey(x => x.FacultyId).OnDelete(DeleteBehavior.SetNull);
        });
        b.Entity<Department>(e =>
        {
            e.ToTable("departments");
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Description).HasMaxLength(2000);
        });
        b.Entity<FacultyMember>(e =>
        {
            e.ToTable("faculty");
            e.HasIndex(x => x.EmployeeNo).IsUnique();
            e.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        });
        b.Entity<Student>(e =>
        {
            e.ToTable("students");
            e.HasIndex(x => x.RollNo).IsUnique();
            e.HasIndex(x => new { x.DepartmentId, x.CurrentSemester });
            e.Property(x => x.Address).HasMaxLength(1000);
            e.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        });
        b.Entity<Course>(e =>
        {
            e.ToTable("courses");
            e.HasIndex(x => x.Code).IsUnique();
            e.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne<FacultyMember>().WithMany().HasForeignKey(x => x.FacultyId).OnDelete(DeleteBehavior.SetNull);
        });
        b.Entity<Enrollment>(e =>
        {
            e.ToTable("enrollments");
            e.HasIndex(x => new { x.StudentId, x.CourseId, x.AcademicYear }).IsUnique();
            e.HasOne<Student>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<AttendanceRecord>(e =>
        {
            e.ToTable("attendance");
            e.HasIndex(x => new { x.CourseId, x.StudentId, x.Date }).IsUnique();
            e.HasOne<Student>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.MarkedByUserId).OnDelete(DeleteBehavior.SetNull);
        });
        b.Entity<Exam>(e =>
        {
            e.ToTable("exams");
            e.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<Mark>(e =>
        {
            e.ToTable("marks");
            e.HasIndex(x => new { x.ExamId, x.StudentId }).IsUnique();
            e.HasOne<Exam>().WithMany().HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Student>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<FeeInvoice>(e =>
        {
            e.ToTable("fee_invoices");
            e.HasIndex(x => new { x.StudentId, x.Status });
            e.Ignore(x => x.Balance);
            e.HasOne<Student>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<Payment>(e =>
        {
            e.ToTable("payments");
            e.HasOne<FeeInvoice>().WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.ReceivedByUserId).OnDelete(DeleteBehavior.SetNull);
        });
        b.Entity<CollegeSetting>(e =>
        {
            e.ToTable("settings");
            e.HasKey(x => x.Key);
            e.Property(x => x.Key).HasMaxLength(100);
            e.Property(x => x.Value).HasMaxLength(100_000);
        });
    }
}
