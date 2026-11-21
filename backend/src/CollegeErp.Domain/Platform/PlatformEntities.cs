using CollegeErp.Domain.Common;

namespace CollegeErp.Domain.Platform;

/// <summary>A platform-level administrator who onboards and manages colleges.</summary>
public class SuperAdmin : Entity, IAuditable
{
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Registry entry for one college (tenant). The college's actual data lives in
/// its own, separate database named <see cref="DatabaseName"/>.
/// </summary>
public class College : Entity, IAuditable
{
    /// <summary>Short URL-safe identifier used in API routes: /api/t/{code}/...</summary>
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string DatabaseName { get; set; } = "";
    public string Status { get; set; } = CollegeStatus.Active;
    public string Plan { get; set; } = "standard";
    /// <summary>Maximum number of students allowed by the plan; null = unlimited.</summary>
    public int? MaxStudents { get; set; }
    public int SchemaVersion { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsActive => Status == CollegeStatus.Active;
}
