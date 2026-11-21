using System.ComponentModel.DataAnnotations;
using CollegeErp.Application.Abstractions;
using CollegeErp.Application.Campus.Crud;
using CollegeErp.Application.Common;
using CollegeErp.Application.Platform;
using CollegeErp.Domain.Campus;
using CollegeErp.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace CollegeErp.Application.Campus.Identity;

public sealed record UserDto(
    int Id, string Email, string FullName, string Role, bool IsActive, int? StudentId, int? FacultyId, DateTime CreatedAt)
{
    public static UserDto From(User u) =>
        new(u.Id, u.Email, u.FullName, u.Role, u.IsActive, u.StudentId, u.FacultyId, u.CreatedAt);
}

public sealed record CollegeProfileDto(UserDto User, string CollegeCode, string CollegeName);

public sealed class CreateUserRequest
{
    [Required, EmailAddress] public string Email { get; init; } = "";
    [Required, StringLength(255)] public string FullName { get; init; } = "";
    [Required, StringLength(72, MinimumLength = 8)] public string Password { get; init; } = "";
    [Required, RegularExpression("^(college_admin|faculty|accountant|student)$")] public string Role { get; init; } = "";
    public int? StudentId { get; init; }
    public int? FacultyId { get; init; }
}

public sealed class UpdateUserRequest
{
    [Required, StringLength(255)] public string FullName { get; init; } = "";
    [Required, RegularExpression("^(college_admin|faculty|accountant|student)$")] public string Role { get; init; } = "";
    public bool IsActive { get; init; } = true;
    public int? StudentId { get; init; }
    public int? FacultyId { get; init; }
    /// <summary>Optional: set to reset the password.</summary>
    [StringLength(72, MinimumLength = 8)] public string? Password { get; init; }
}

public interface ICollegeAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<CollegeProfileDto> MeAsync(CancellationToken ct = default);
}

public sealed class CollegeAuthService(
    ICollegeDbContext db, ICurrentTenant tenant, ICurrentUser currentUser, IPasswordHasher hasher, ITokenService tokens)
    : ICollegeAuthService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null || !user.IsActive || !hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException();

        var token = tokens.CreateCollegeToken(tenant.College.Code, user);
        return new LoginResponse(token.Token, token.ExpiresAt, Profile(user));
    }

    public async Task<CollegeProfileDto> MeAsync(CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([currentUser.UserId], ct) ?? throw new UnauthorizedException("User not found.");
        return Profile(user);
    }

    private CollegeProfileDto Profile(User u) => new(UserDto.From(u), tenant.College.Code, tenant.College.Name);
}

public interface IUserService
{
    Task<PagedResult<UserDto>> ListAsync(ListQuery query, CancellationToken ct = default);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class UserService(
    ICollegeDbContext db, IPasswordHasher hasher, ICurrentUser currentUser, ReferenceGuard guard, TimeProvider clock)
    : IUserService
{
    public async Task<PagedResult<UserDto>> ListAsync(ListQuery query, CancellationToken ct = default)
    {
        var q = db.Users.AsNoTracking();
        if (query.Search is { Length: > 0 } s)
        {
            s = s.ToLower();
            q = q.Where(u => u.Email.Contains(s) || u.FullName.ToLower().Contains(s));
        }
        if (query.StringFilter("role") is { } role) q = q.Where(u => u.Role == role);
        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(u => u.Id).Skip(query.SafeSkip).Take(query.SafeTake).ToListAsync(ct);
        return new PagedResult<UserDto>(items.Select(UserDto.From).ToList(), total, query.SafeSkip, query.SafeTake);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest r, CancellationToken ct = default)
    {
        var email = r.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("A user with this email already exists.");
        await ValidateLinksAsync(r.Role, r.StudentId, r.FacultyId, ct);

        var user = new User
        {
            Email = email, FullName = r.FullName.Trim(), Role = r.Role,
            PasswordHash = hasher.Hash(r.Password), StudentId = r.StudentId, FacultyId = r.FacultyId,
            CreatedAt = clock.GetUtcNow().UtcDateTime,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return UserDto.From(user);
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserRequest r, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([id], ct) ?? throw NotFoundException.For<User>(id);
        if (user.Id == currentUser.UserId && (!r.IsActive || r.Role != Roles.CollegeAdmin))
            throw new ConflictException("You cannot deactivate or demote your own account.");
        await ValidateLinksAsync(r.Role, r.StudentId, r.FacultyId, ct);

        user.FullName = r.FullName.Trim();
        user.Role = r.Role;
        user.IsActive = r.IsActive;
        user.StudentId = r.StudentId;
        user.FacultyId = r.FacultyId;
        if (!string.IsNullOrEmpty(r.Password)) user.PasswordHash = hasher.Hash(r.Password);
        await db.SaveChangesAsync(ct);
        return UserDto.From(user);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        if (id == currentUser.UserId) throw new ConflictException("You cannot delete your own account.");
        var user = await db.Users.FindAsync([id], ct) ?? throw NotFoundException.For<User>(id);
        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
    }

    private async Task ValidateLinksAsync(string role, int? studentId, int? facultyId, CancellationToken ct)
    {
        if (role == Roles.Student && studentId is null)
            throw new InvalidInputException("studentId", "Student accounts must be linked to a student record.");
        if (role == Roles.Faculty && facultyId is null)
            throw new InvalidInputException("facultyId", "Faculty accounts must be linked to a faculty record.");
        await guard.EnsureExistsAsync<Student>(studentId, "studentId", ct);
        await guard.EnsureExistsAsync<FacultyMember>(facultyId, "facultyId", ct);
    }
}
