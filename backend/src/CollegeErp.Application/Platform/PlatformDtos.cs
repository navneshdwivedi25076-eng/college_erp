using System.ComponentModel.DataAnnotations;

namespace CollegeErp.Application.Platform;

public sealed class LoginRequest
{
    [Required, EmailAddress] public string Email { get; init; } = "";
    [Required, StringLength(72, MinimumLength = 6)] public string Password { get; init; } = "";
}

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAt, object Profile);

public sealed class CreateCollegeRequest
{
    /// <summary>Lowercase letters, digits and underscores; becomes part of the URL and DB name.</summary>
    [Required, RegularExpression("^[a-z0-9][a-z0-9_]{1,38}[a-z0-9]$",
        ErrorMessage = "Code must be 3-40 chars of lowercase letters, digits or underscores.")]
    public string Code { get; init; } = "";
    [Required, StringLength(255)] public string Name { get; init; } = "";
    [StringLength(500)] public string? Address { get; init; }
    [EmailAddress] public string? ContactEmail { get; init; }
    [StringLength(50)] public string? ContactPhone { get; init; }
    [StringLength(50)] public string Plan { get; init; } = "standard";
    [Range(1, int.MaxValue)] public int? MaxStudents { get; init; }

    // The first administrator account created inside the new college's database.
    [Required, EmailAddress] public string AdminEmail { get; init; } = "";
    [Required, StringLength(255)] public string AdminFullName { get; init; } = "";
    [Required, StringLength(72, MinimumLength = 8)] public string AdminPassword { get; init; } = "";
}

public sealed class UpdateCollegeRequest
{
    [Required, StringLength(255)] public string Name { get; init; } = "";
    [StringLength(500)] public string? Address { get; init; }
    [EmailAddress] public string? ContactEmail { get; init; }
    [StringLength(50)] public string? ContactPhone { get; init; }
    [Required, StringLength(50)] public string Plan { get; init; } = "standard";
    [Range(1, int.MaxValue)] public int? MaxStudents { get; init; }
}

public sealed record CollegeDto(
    int Id, string Code, string Name, string? Address, string? ContactEmail, string? ContactPhone,
    string DatabaseName, string Status, string Plan, int? MaxStudents, int SchemaVersion, DateTime CreatedAt);

public sealed record CollegeStatsDto(int CollegeId, int Students, int Faculty, int Courses, int Departments, int Users);

public sealed class CreateSuperAdminRequest
{
    [Required, EmailAddress] public string Email { get; init; } = "";
    [Required, StringLength(255)] public string FullName { get; init; } = "";
    [Required, StringLength(72, MinimumLength = 8)] public string Password { get; init; } = "";
}

public sealed record SuperAdminDto(int Id, string Email, string FullName, DateTime CreatedAt);

public sealed record MigrationReport(int Upgraded, IReadOnlyList<string> Failures);
