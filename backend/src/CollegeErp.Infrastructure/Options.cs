namespace CollegeErp.Infrastructure;

public sealed class DatabaseOptions
{
    public const string Section = "Database";

    /// <summary>"Sqlite" (zero-setup, dev/small installs) or "PostgreSql" (production).</summary>
    public string Provider { get; set; } = "Sqlite";

    /// <summary>Connection string of the platform (master) database.</summary>
    public string MasterConnectionString { get; set; } = "Data Source=data/master.db";

    /// <summary>
    /// PostgreSQL only: server on which per-college databases are created. The database
    /// name in this string is replaced per college. Defaults to MasterConnectionString.
    /// </summary>
    public string? TenantServerConnectionString { get; set; }

    /// <summary>SQLite only: folder holding one .db file per college.</summary>
    public string SqliteDirectory { get; set; } = "data/colleges";

    /// <summary>Prefix of every college database name, e.g. college_stxavier.</summary>
    public string TenantDatabasePrefix { get; set; } = "college_";

    /// <summary>Run pending schema migrations on every college database at startup.</summary>
    public bool MigrateTenantsOnStartup { get; set; } = true;
}

public sealed class JwtOptions
{
    public const string Section = "Jwt";
    public string Issuer { get; set; } = "college-erp";
    public string Audience { get; set; } = "college-erp";
    /// <summary>At least 32 characters. ALWAYS override in production.</summary>
    public string Secret { get; set; } = "";
    public int ExpiryMinutes { get; set; } = 720;
}

public sealed class BootstrapOptions
{
    public const string Section = "Bootstrap";
    public string SuperAdminEmail { get; set; } = "admin@platform.local";
    public string SuperAdminPassword { get; set; } = "Admin@12345";
}
