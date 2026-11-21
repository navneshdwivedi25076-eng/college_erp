using System.ComponentModel.DataAnnotations;
using CollegeErp.Application.Campus.Crud;
using CollegeErp.Domain.Campus;

namespace CollegeErp.Application.Campus.Academics;

public sealed class DepartmentWrite : IWriteModel<Department>
{
    [Required, StringLength(20)] public string Code { get; init; } = "";
    [Required, StringLength(255)] public string Name { get; init; } = "";
    public string? Description { get; init; }

    public void ApplyTo(Department e)
    {
        e.Code = Code.Trim().ToUpperInvariant();
        e.Name = Name.Trim();
        e.Description = Description;
    }
}

public sealed class FacultyWrite : IWriteModel<FacultyMember>
{
    [Required, StringLength(50)] public string EmployeeNo { get; init; } = "";
    [Required, StringLength(100)] public string FirstName { get; init; } = "";
    [Required, StringLength(100)] public string LastName { get; init; } = "";
    [EmailAddress] public string? Email { get; init; }
    [StringLength(50)] public string? Phone { get; init; }
    [StringLength(100)] public string? Designation { get; init; }
    public int? DepartmentId { get; init; }
    public DateOnly? JoinedOn { get; init; }

    public void ApplyTo(FacultyMember e)
    {
        e.EmployeeNo = EmployeeNo.Trim();
        e.FirstName = FirstName.Trim();
        e.LastName = LastName.Trim();
        e.Email = Email;
        e.Phone = Phone;
        e.Designation = Designation;
        e.DepartmentId = DepartmentId;
        e.JoinedOn = JoinedOn;
    }
}

public sealed class StudentWrite : IWriteModel<Student>
{
    [Required, StringLength(50)] public string RollNo { get; init; } = "";
    [Required, StringLength(100)] public string FirstName { get; init; } = "";
    [Required, StringLength(100)] public string LastName { get; init; } = "";
    [EmailAddress] public string? Email { get; init; }
    [StringLength(50)] public string? Phone { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    [StringLength(20)] public string? Gender { get; init; }
    public int? DepartmentId { get; init; }
    [Range(1900, 3000)] public int? AdmissionYear { get; init; }
    [Range(1, 20)] public int CurrentSemester { get; init; } = 1;
    [RegularExpression("^(active|graduated|dropped|suspended)$")] public string Status { get; init; } = "active";
    public string? Address { get; init; }
    [StringLength(255)] public string? GuardianName { get; init; }
    [StringLength(50)] public string? GuardianPhone { get; init; }

    public void ApplyTo(Student e)
    {
        e.RollNo = RollNo.Trim();
        e.FirstName = FirstName.Trim();
        e.LastName = LastName.Trim();
        e.Email = Email;
        e.Phone = Phone;
        e.DateOfBirth = DateOfBirth;
        e.Gender = Gender;
        e.DepartmentId = DepartmentId;
        e.AdmissionYear = AdmissionYear;
        e.CurrentSemester = CurrentSemester;
        e.Status = Status;
        e.Address = Address;
        e.GuardianName = GuardianName;
        e.GuardianPhone = GuardianPhone;
    }
}

public sealed class CourseWrite : IWriteModel<Course>
{
    [Required, StringLength(20)] public string Code { get; init; } = "";
    [Required, StringLength(255)] public string Title { get; init; } = "";
    [Range(0, 50)] public decimal Credits { get; init; } = 3;
    [Range(1, 20)] public int? Semester { get; init; }
    public int? DepartmentId { get; init; }
    public int? FacultyId { get; init; }

    public void ApplyTo(Course e)
    {
        e.Code = Code.Trim().ToUpperInvariant();
        e.Title = Title.Trim();
        e.Credits = Credits;
        e.Semester = Semester;
        e.DepartmentId = DepartmentId;
        e.FacultyId = FacultyId;
    }
}

public sealed class ExamWrite : IWriteModel<Exam>
{
    [Required] public int CourseId { get; init; }
    [Required, StringLength(100)] public string Name { get; init; } = "";
    [Range(1, 10000)] public decimal MaxMarks { get; init; } = 100;
    public DateOnly? ExamDate { get; init; }

    public void ApplyTo(Exam e)
    {
        e.CourseId = CourseId;
        e.Name = Name.Trim();
        e.MaxMarks = MaxMarks;
        e.ExamDate = ExamDate;
    }
}
