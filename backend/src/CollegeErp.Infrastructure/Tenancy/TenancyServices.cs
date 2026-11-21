using CollegeErp.Application.Abstractions;
using CollegeErp.Infrastructure.Databases;
using CollegeErp.Infrastructure.Persistence;
using CollegeErp.Infrastructure.Tenancy.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using IRelationalDatabaseCreator = Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CollegeErp.Infrastructure.Tenancy;

/// <summary>Write side of the current tenant; only the tenant-resolution middleware uses it.</summary>
public interface ITenantSetter
{
    void Set(CollegeInfo college);
}

public sealed class CurrentTenant : ICurrentTenant, ITenantSetter
{
    private CollegeInfo? _college;

    public CollegeInfo College =>
        _college ?? throw new InvalidOperationException("No college has been resolved for this request.");

    public bool IsResolved => _college is not null;

    public void Set(CollegeInfo college) => _college = college;
}

/// <summary>College lookup by code with a short in-memory cache (avoids a master-DB hit per request).</summary>
public sealed class CachedCollegeDirectory(IMasterDbContext master, IMemoryCache cache) : ICollegeDirectory
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);
    private static string Key(string code) => $"college:{code}";

    public async Task<CollegeInfo?> FindByCodeAsync(string code, CancellationToken ct = default)
    {
        code = code.ToLowerInvariant();
        if (cache.TryGetValue(Key(code), out CollegeInfo? cached)) return cached;
        var info = await master.Colleges.AsNoTracking().Where(c => c.Code == code)
            .Select(c => new CollegeInfo(c.Id, c.Code, c.Name, c.DatabaseName, c.Status, c.Plan, c.MaxStudents))
            .FirstOrDefaultAsync(ct);
        if (info is not null) cache.Set(Key(code), info, Ttl);
        return info;
    }

    public void Invalidate(string code) => cache.Remove(Key(code.ToLowerInvariant()));
}

public sealed class CollegeDbContextFactory(IDatabaseEngine provider) : ICollegeDbContextFactory
{
    public ICollegeDbContext Create(string databaseName) => CreateConcrete(databaseName);

    public CollegeDbContext CreateConcrete(string databaseName)
    {
        var builder = new DbContextOptionsBuilder<CollegeDbContext>();
        provider.Configure(builder, provider.TenantConnectionString(databaseName));
        return new CollegeDbContext(builder.Options);
    }
}

/// <summary>Database-per-college lifecycle: create, migrate, drop.</summary>
public sealed class TenantDatabaseManager(
    IDatabaseEngine provider,
    CollegeDbContextFactory contexts,
    TenantMigrator migrator,
    IOptions<DatabaseOptions> options) : ITenantDatabaseManager
{
    public string DatabaseNameFor(string collegeCode) =>
        $"{options.Value.TenantDatabasePrefix}{collegeCode.ToLowerInvariant()}";

    public async Task<bool> ExistsAsync(string databaseName, CancellationToken ct = default)
    {
        await using var db = contexts.CreateConcrete(databaseName);
        return await db.Database.GetService<IRelationalDatabaseCreator>().ExistsAsync(ct);
    }

    public async Task<int> CreateAsync(string databaseName, CancellationToken ct = default)
    {
        await using var db = contexts.CreateConcrete(databaseName);
        // Creates the database itself (file for SQLite, CREATE DATABASE for PostgreSQL) and all tables.
        if (!await db.Database.EnsureCreatedAsync(ct))
            throw new InvalidOperationException($"Database '{databaseName}' already exists.");
        return await migrator.StampLatestAsync(db, ct);
    }

    public async Task<int> MigrateAsync(string databaseName, CancellationToken ct = default)
    {
        await using var db = contexts.CreateConcrete(databaseName);
        return await migrator.MigrateAsync(db, ct);
    }

    public async Task DropAsync(string databaseName, CancellationToken ct = default)
    {
        provider.ClearPool(provider.TenantConnectionString(databaseName));
        await using var db = contexts.CreateConcrete(databaseName);
        await db.Database.EnsureDeletedAsync(ct);
    }
}
