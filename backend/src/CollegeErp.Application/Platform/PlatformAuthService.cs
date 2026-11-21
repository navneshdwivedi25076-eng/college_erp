using CollegeErp.Application.Abstractions;
using CollegeErp.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace CollegeErp.Application.Platform;

public interface IPlatformAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

public sealed class PlatformAuthService(IMasterDbContext db, IPasswordHasher hasher, ITokenService tokens)
    : IPlatformAuthService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var admin = await db.SuperAdmins.FirstOrDefaultAsync(a => a.Email == email, ct);
        if (admin is null || !hasher.Verify(request.Password, admin.PasswordHash))
            throw new UnauthorizedException();

        var token = tokens.CreatePlatformToken(admin);
        return new LoginResponse(token.Token, token.ExpiresAt,
            new SuperAdminDto(admin.Id, admin.Email, admin.FullName, admin.CreatedAt));
    }
}
