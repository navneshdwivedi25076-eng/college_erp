using CollegeErp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CollegeErp.Infrastructure.Tenancy.Migrations;

/// <summary>
/// A versioned schema change applied to EVERY college database.
///
/// How to evolve the schema after colleges already exist:
///   1. Change the entity / CollegeDbContext (new colleges get it automatically).
///   2. Add a class implementing ITenantMigration with the next Version that brings
///      EXISTING databases to the same shape, e.g.
///        public sealed class AddBloodGroup : ITenantMigration {
///            public int Version => 2;
///            public string Description => "Add blood_group to students";
///            public Task UpAsync(CollegeDbContext db, CancellationToken ct) =>
///                db.Database.ExecuteSqlRawAsync("ALTER TABLE students ADD COLUMN \"BloodGroup\" VARCHAR(5)", ct);
///        }
///   3. Register it in DependencyInjection (AddTenantMigration&lt;AddBloodGroup&gt;()).
///   Every college is upgraded on next startup or via POST /api/platform/colleges/migrate.
/// </summary>
public interface ITenantMigration
{
    int Version { get; }
    string Description { get; }
    Task UpAsync(CollegeDbContext db, CancellationToken ct);
}

/// <summary>Version 1 is the baseline schema created from the EF model.</summary>
public sealed class InitialSchema : ITenantMigration
{
    public int Version => 1;
    public string Description => "Initial schema";
    public Task UpAsync(CollegeDbContext db, CancellationToken ct) => db.Database.EnsureCreatedAsync(ct);
}

public sealed class TenantMigrator(IEnumerable<ITenantMigration> migrations, TimeProvider clock)
{
    private readonly IReadOnlyList<ITenantMigration> _ordered = migrations.OrderBy(m => m.Version).ToList();

    public int LatestVersion => _ordered.Count == 0 ? 0 : _ordered[^1].Version;

    /// <summary>Marks a freshly created database (built from the current model) as fully up to date.</summary>
    public async Task<int> StampLatestAsync(CollegeDbContext db, CancellationToken ct)
    {
        db.SchemaVersions.AddRange(_ordered.Select(m => new SchemaVersionEntryFactory(clock).Create(m)));
        await db.SaveChangesAsync(ct);
        return LatestVersion;
    }

    public async Task<int> MigrateAsync(CollegeDbContext db, CancellationToken ct)
    {
        var current = await db.SchemaVersions.MaxAsync(v => (int?)v.Version, ct) ?? 0;
        foreach (var migration in _ordered.Where(m => m.Version > current))
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await migration.UpAsync(db, ct);
            db.SchemaVersions.Add(new SchemaVersionEntryFactory(clock).Create(migration));
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            current = migration.Version;
        }
        return current;
    }

    private readonly struct SchemaVersionEntryFactory(TimeProvider clock)
    {
        public Domain.Campus.SchemaVersionEntry Create(ITenantMigration m) => new()
        {
            Version = m.Version, Description = m.Description, AppliedAt = clock.GetUtcNow().UtcDateTime,
        };
    }
}
