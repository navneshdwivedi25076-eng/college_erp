namespace CollegeErp.Domain.Common;

/// <summary>Roles a user can hold INSIDE a college.</summary>
public static class Roles
{
    public const string CollegeAdmin = "college_admin";
    public const string Faculty = "faculty";
    public const string Accountant = "accountant";
    public const string Student = "student";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string> { CollegeAdmin, Faculty, Accountant, Student };
}

public static class CollegeStatus
{
    public const string Active = "active";
    public const string Suspended = "suspended";
}

public static class AttendanceStatus
{
    public const string Present = "present";
    public const string Absent = "absent";
    public const string Late = "late";
    public const string Excused = "excused";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string> { Present, Absent, Late, Excused };
}

public static class InvoiceStatus
{
    public const string Unpaid = "unpaid";
    public const string Partial = "partial";
    public const string Paid = "paid";
}
