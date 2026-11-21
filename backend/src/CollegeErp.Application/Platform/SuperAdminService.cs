using CollegeErp.Application.Abstractions;
using CollegeErp.Application.Common;
using CollegeErp.Domain.Platform;
using Microsoft.EntityFrameworkCore;

namespace CollegeErp.Application.Platform;

public interface ISuperAdminService
{
    Task<IReadOnlyList<SuperAdminDto>> ListAsync(CancellationToken ct = default);
    Task<SuperAdminDto> CreateAsync(CreateSuperAdminRequest request, CancellationToken ct = default);
    /// <summary>Creates the first super admin if none exists (used at startup).</summary>
    Task EnsureBootstrapAdminAsync(string email, string password, CancellationToken ct = default);
}

public sealed class SuperAdminService(IMasterDbContext db, IPasswordHasher hasher, TimeProvider clock)
    : ISuperAdminService
{
    public async Task<IReadOnlyList<SuperAdminDto>> ListAsync(CancellationToken ct = default) =>
        await db.SuperAdmins.AsNoTracking().OrderBy(a => a.Email)
            .Select(a => new SuperAdminDto(a.Id, a.Email, a.FullName, a.CreatedAt)).ToListAsync(ct);

    public async Task<SuperAdminDto> CreateAsync(CreateSuperAdminRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.SuperAdmins.AnyAsync(a => a.Email == email, ct))
            throw new ConflictException("A platform admin with this email already exists.");
        var admin = new SuperAdmin
        {
            Email = email, FullName = request.FullName.Trim(),
            PasswordHash = hasher.Hash(request.Password), CreatedAt = clock.GetUtcNow().UtcDateTime,
        };
        db.SuperAdmins.Add(admin);
        await db.SaveChangesAsync(ct);
        return new SuperAdminDto(admin.Id, admin.Email, admin.FullName, admin.CreatedAt);
    }

    public async Task EnsureBootstrapAdminAsync(string email, string password, CancellationToken ct = default)
    {
        if (await db.SuperAdmins.AnyAsync(ct)) return;
        db.SuperAdmins.Add(new SuperAdmin
        {
            Email = email.Trim().ToLowerInvariant(), FullName = "Platform Administrator",
            PasswordHash = hasher.Hash(password), CreatedAt = clock.GetUtcNow().UtcDateTime,
        });
        await db.SaveChangesAsync(ct);
    }
}
