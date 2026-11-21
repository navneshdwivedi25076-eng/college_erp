using CollegeErp.Application.Abstractions;
using CollegeErp.Application.Common;
using CollegeErp.Domain.Campus;
using CollegeErp.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CollegeEntity = CollegeErp.Domain.Platform.College;

namespace CollegeErp.Application.Platform;

public interface ICollegeOnboardingService
{
    Task<CollegeDto> CreateCollegeAsync(CreateCollegeRequest request, CancellationToken ct = default);
}

/// <summary>
/// Onboards a new college: registers it, provisions a dedicated database, applies
/// the schema and seeds its first administrator. Rolls back on any failure.
/// </summary>
public sealed class CollegeOnboardingService(
    IMasterDbContext master,
    ITenantDatabaseManager databases,
    ICollegeDbContextFactory collegeContexts,
    IPasswordHasher hasher,
    TimeProvider clock,
    ILogger<CollegeOnboardingService> logger) : ICollegeOnboardingService
{
    public async Task<CollegeDto> CreateCollegeAsync(CreateCollegeRequest request, CancellationToken ct = default)
    {
        var code = request.Code.Trim().ToLowerInvariant();
        if (await master.Colleges.AnyAsync(c => c.Code == code, ct))
            throw new ConflictException($"A college with code '{code}' already exists.");

        var dbName = databases.DatabaseNameFor(code);
        if (await databases.ExistsAsync(dbName, ct))
            throw new ConflictException($"Database '{dbName}' already exists.");

        var schemaVersion = await databases.CreateAsync(dbName, ct);
        try
        {
            await using (var collegeDb = collegeContexts.Create(dbName))
            {
                collegeDb.Users.Add(new User
                {
                    Email = request.AdminEmail.Trim().ToLowerInvariant(),
                    FullName = request.AdminFullName.Trim(),
                    PasswordHash = hasher.Hash(request.AdminPassword),
                    Role = Roles.CollegeAdmin,
                    CreatedAt = clock.GetUtcNow().UtcDateTime,
                });
                await collegeDb.SaveChangesAsync(ct);
            }

            var college = new CollegeEntity
            {
                Code = code,
                Name = request.Name.Trim(),
                Address = request.Address,
                ContactEmail = request.ContactEmail,
                ContactPhone = request.ContactPhone,
                Plan = request.Plan,
                MaxStudents = request.MaxStudents,
                DatabaseName = dbName,
                Status = CollegeStatus.Active,
                SchemaVersion = schemaVersion,
                CreatedAt = clock.GetUtcNow().UtcDateTime,
            };
            master.Colleges.Add(college);
            await master.SaveChangesAsync(ct);

            logger.LogInformation("Provisioned college {Code} with database {Db}", code, dbName);
            return college.ToDto();
        }
        catch
        {
            logger.LogWarning("Onboarding of {Code} failed, dropping database {Db}", code, dbName);
            await databases.DropAsync(dbName, CancellationToken.None);
            throw;
        }
    }
}

internal static class CollegeMappings
{
    public static CollegeDto ToDto(this CollegeEntity c) => new(
        c.Id, c.Code, c.Name, c.Address, c.ContactEmail, c.ContactPhone,
        c.DatabaseName, c.Status, c.Plan, c.MaxStudents, c.SchemaVersion, c.CreatedAt);
}
