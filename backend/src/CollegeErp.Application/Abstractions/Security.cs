using CollegeErp.Domain.Campus;
using CollegeErp.Domain.Platform;

namespace CollegeErp.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public sealed record AccessToken(string Token, DateTime ExpiresAt);

public interface ITokenService
{
    AccessToken CreatePlatformToken(SuperAdmin admin);
    AccessToken CreateCollegeToken(string collegeCode, User user);
}

/// <summary>The authenticated college user of the current request.</summary>
public interface ICurrentUser
{
    int UserId { get; }
    string Role { get; }
    int? StudentId { get; }
    int? FacultyId { get; }
}
