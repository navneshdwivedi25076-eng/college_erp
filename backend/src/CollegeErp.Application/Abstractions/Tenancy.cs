namespace CollegeErp.Application.Abstractions;

/// <summary>Immutable snapshot of the college the current request belongs to.</summary>
public sealed record CollegeInfo(
    int Id, string Code, string Name, string DatabaseName, string Status, string Plan, int? MaxStudents);

/// <summary>Read-only access to the tenant resolved for the current request.</summary>
public interface ICurrentTenant
{
    CollegeInfo College { get; }
    bool IsResolved { get; }
}

/// <summary>Looks colleges up by code (implementations may cache).</summary>
public interface ICollegeDirectory
{
    Task<CollegeInfo?> FindByCodeAsync(string code, CancellationToken ct = default);
    void Invalidate(string code);
}

/// <summary>Creates, upgrades and drops per-college databases.</summary>
public interface ITenantDatabaseManager
{
    /// <summary>Physical database name for a college code (e.g. "college_stxavier").</summary>
    string DatabaseNameFor(string collegeCode);
    Task<bool> ExistsAsync(string databaseName, CancellationToken ct = default);
    /// <summary>Creates an empty database and applies the full schema. Returns the schema version.</summary>
    Task<int> CreateAsync(string databaseName, CancellationToken ct = default);
    /// <summary>Applies pending migrations. Returns the resulting schema version.</summary>
    Task<int> MigrateAsync(string databaseName, CancellationToken ct = default);
    Task DropAsync(string databaseName, CancellationToken ct = default);
}

/// <summary>Opens a context on a specific college database (used outside a tenant request, e.g. onboarding).</summary>
public interface ICollegeDbContextFactory
{
    ICollegeDbContext Create(string databaseName);
}
