using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CollegeErp.Application.Abstractions;
using CollegeErp.Domain.Campus;
using CollegeErp.Domain.Platform;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CollegeErp.Infrastructure.Security;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);

    public bool Verify(string password, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(password, hash); }
        catch (BCrypt.Net.SaltParseException) { return false; }
    }
}

public static class AppClaims
{
    public const string Scope = "scope";
    public const string College = "college";
    public const string StudentId = "student_id";
    public const string FacultyId = "faculty_id";
    public const string PlatformScope = "platform";
    public const string CollegeScope = "college";
}

public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider clock) : ITokenService
{
    private readonly JwtOptions _o = options.Value;

    public AccessToken CreatePlatformToken(SuperAdmin admin) => Create(
    [
        new(JwtRegisteredClaimNames.Sub, admin.Id.ToString()),
        new(JwtRegisteredClaimNames.Email, admin.Email),
        new(AppClaims.Scope, AppClaims.PlatformScope),
    ]);

    public AccessToken CreateCollegeToken(string collegeCode, User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(AppClaims.Scope, AppClaims.CollegeScope),
            new(AppClaims.College, collegeCode),
            new(ClaimTypes.Role, user.Role),
        };
        if (user.StudentId is { } sid) claims.Add(new(AppClaims.StudentId, sid.ToString()));
        if (user.FacultyId is { } fid) claims.Add(new(AppClaims.FacultyId, fid.ToString()));
        return Create(claims);
    }

    private AccessToken Create(IEnumerable<Claim> claims)
    {
        var expires = clock.GetUtcNow().UtcDateTime.AddMinutes(_o.ExpiryMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_o.Secret));
        var token = new JwtSecurityToken(_o.Issuer, _o.Audience, claims,
            notBefore: clock.GetUtcNow().UtcDateTime, expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
