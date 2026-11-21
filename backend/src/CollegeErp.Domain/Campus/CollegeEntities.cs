using CollegeErp.Domain.Common;

namespace CollegeErp.Domain.Campus;

// Everything in this file lives in EACH college's private database.

public class User : Entity, IAuditable
{
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = Roles.CollegeAdmin;
    public bool IsActive { get; set; } = true;
    /// <summary>Set when Role == student: links the login to the student record.</summary>
    public int? StudentId { get; set; }
    /// <summary>Set when Role == faculty: links the login to the faculty record.</summary>
    public int? FacultyId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Department : Entity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}

public class FacultyMember : Entity
{
    public string EmployeeNo { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Designation { get; set; }
    public int? DepartmentId { get; set; }
    public DateOnly? JoinedOn { get; set; }
}

public class Student : Entity, IAuditable
{
    public string RollNo { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public int? DepartmentId { get; set; }
    public int? AdmissionYear { get; set; }
    public int CurrentSemester { get; set; } = 1;
    public string Status { get; set; } = "active";
    public string? Address { get; set; }
    public string? GuardianName { get; set; }
    public string? GuardianPhone { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Course : Entity
{
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal Credits { get; set; } = 3;
    public int? Semester { get; set; }
    public int? DepartmentId { get; set; }
    public int? FacultyId { get; set; }
}

public class Enrollment : Entity
{
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string AcademicYear { get; set; } = "";
    public DateTime EnrolledOn { get; set; }
}

public class AttendanceRecord : Entity
{
    public int CourseId { get; set; }
    public int StudentId { get; set; }
    public DateOnly Date { get; set; }
    public string Status { get; set; } = AttendanceStatus.Present;
    public int? MarkedByUserId { get; set; }
}

public class Exam : Entity
{
    public int CourseId { get; set; }
    public string Name { get; set; } = "";
    public decimal MaxMarks { get; set; } = 100;
    public DateOnly? ExamDate { get; set; }
}

public class Mark : Entity
{
    public int ExamId { get; set; }
    public int StudentId { get; set; }
    public decimal MarksObtained { get; set; }
    public string? Remarks { get; set; }
}

public class FeeInvoice : Entity, IAuditable
{
    public int StudentId { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal AmountPaid { get; set; }
    public DateOnly? DueDate { get; set; }
    public string Status { get; set; } = InvoiceStatus.Unpaid;
    public DateTime CreatedAt { get; set; }

    public decimal Balance => Amount - AmountPaid;

    /// <summary>Domain rule: applying a payment updates the paid amount and status.</summary>
    public void ApplyPayment(decimal amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Payment must be positive.");
        if (amount > Balance) throw new InvalidOperationException($"Payment exceeds outstanding balance ({Balance}).");
        AmountPaid += amount;
        Status = AmountPaid >= Amount ? InvoiceStatus.Paid : InvoiceStatus.Partial;
    }
}

public class Payment : Entity
{
    public int InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "cash";
    public string? Reference { get; set; }
    public DateTime PaidOn { get; set; }
    public int? ReceivedByUserId { get; set; }
}

/// <summary>Free-form per-college configuration (JSON value), e.g. grading scale, branding.</summary>
public class CollegeSetting
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

/// <summary>Tracks which schema migrations have been applied to this college database.</summary>
public class SchemaVersionEntry
{
    public int Version { get; set; }
    public string Description { get; set; } = "";
    public DateTime AppliedAt { get; set; }
}
