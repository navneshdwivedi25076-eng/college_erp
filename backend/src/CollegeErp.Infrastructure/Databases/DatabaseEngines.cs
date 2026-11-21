using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CollegeErp.Infrastructure.Databases;

/// <summary>
/// Strategy for a database engine. To support another engine (e.g. SQL Server),
/// add an implementation and register it — nothing else changes (Open/Closed).
/// </summary>
public interface IDatabaseEngine
{
    string Name { get; }
    string MasterConnectionString { get; }
    string TenantConnectionString(string databaseName);
    void Configure(DbContextOptionsBuilder builder, string connectionString);
    /// <summary>Releases pooled connections so a database can be dropped.</summary>
    void ClearPool(string connectionString);
}

public sealed class SqliteDatabaseEngine(IOptions<DatabaseOptions> options) : IDatabaseEngine
{
    private readonly DatabaseOptions _o = options.Value;

    public string Name => "Sqlite";

    public string MasterConnectionString
    {
        get
        {
            var b = new SqliteConnectionStringBuilder(_o.MasterConnectionString) { ForeignKeys = true };
            EnsureDirectory(b.DataSource);
            return b.ToString();
        }
    }

    public string TenantConnectionString(string databaseName)
    {
        var path = Path.Combine(_o.SqliteDirectory, $"{databaseName}.db");
        EnsureDirectory(path);
        return new SqliteConnectionStringBuilder { DataSource = path, ForeignKeys = true }.ToString();
    }

    public void Configure(DbContextOptionsBuilder builder, string connectionString) =>
        builder.UseSqlite(connectionString);

    public void ClearPool(string connectionString)
    {
        using var conn = new SqliteConnection(connectionString);
        SqliteConnection.ClearPool(conn);
    }

    private static void EnsureDirectory(string filePath)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    }
}

public sealed class PostgreSqlDatabaseEngine(IOptions<DatabaseOptions> options) : IDatabaseEngine
{
    private readonly DatabaseOptions _o = options.Value;

    public string Name => "PostgreSql";

    public string MasterConnectionString => _o.MasterConnectionString;

    public string TenantConnectionString(string databaseName) =>
        new NpgsqlConnectionStringBuilder(_o.TenantServerConnectionString ?? _o.MasterConnectionString)
        {
            Database = databaseName,
            // Keep per-college pools small and release idle connections quickly so
            // thousands of colleges can share one server.
            MaxPoolSize = 20,
            ConnectionIdleLifetime = 60,
        }.ToString();

    public void Configure(DbContextOptionsBuilder builder, string connectionString) =>
        builder.UseNpgsql(connectionString, npg => npg.EnableRetryOnFailure(3));

    public void ClearPool(string connectionString)
    {
        using var conn = new NpgsqlConnection(connectionString);
        NpgsqlConnection.ClearPool(conn);
    }
}
