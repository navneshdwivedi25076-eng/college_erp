using CollegeErp.Application.Abstractions;
using CollegeErp.Infrastructure.Databases;
using CollegeErp.Infrastructure.Persistence;
using CollegeErp.Infrastructure.Security;
using CollegeErp.Infrastructure.Tenancy;
using CollegeErp.Infrastructure.Tenancy.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CollegeErp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<DatabaseOptions>(config.GetSection(DatabaseOptions.Section));
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.Section));
        services.Configure<BootstrapOptions>(config.GetSection(BootstrapOptions.Section));
        services.AddMemoryCache();

        // Database engine strategy, selected by configuration (Database:Provider).
        services.AddSingleton<SqliteDatabaseEngine>();
        services.AddSingleton<PostgreSqlDatabaseEngine>();
        services.AddSingleton<IDatabaseEngine>(sp =>
        {
            var name = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.Provider;
            return name.ToLowerInvariant() switch
            {
                "sqlite" => sp.GetRequiredService<SqliteDatabaseEngine>(),
                "postgresql" or "postgres" or "npgsql" => sp.GetRequiredService<PostgreSqlDatabaseEngine>(),
                _ => throw new InvalidOperationException($"Unsupported database provider '{name}'."),
            };
        });

        // Master (platform) database
        services.AddDbContext<MasterDbContext>((sp, b) =>
        {
            var p = sp.GetRequiredService<IDatabaseEngine>();
            p.Configure(b, p.MasterConnectionString);
        });
        services.AddScoped<IMasterDbContext>(sp => sp.GetRequiredService<MasterDbContext>());

        // Tenancy: the per-request college + a DbContext bound to THAT college's database
        services.AddScoped<CurrentTenant>();
        services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<CurrentTenant>());
        services.AddScoped<ITenantSetter>(sp => sp.GetRequiredService<CurrentTenant>());
        services.AddScoped<ICollegeDirectory, CachedCollegeDirectory>();
        services.AddDbContext<CollegeDbContext>((sp, b) =>
        {
            var p = sp.GetRequiredService<IDatabaseEngine>();
            var tenant = sp.GetRequiredService<ICurrentTenant>();
            p.Configure(b, p.TenantConnectionString(tenant.College.DatabaseName));
        });
        services.AddScoped<ICollegeDbContext>(sp => sp.GetRequiredService<CollegeDbContext>());
        services.AddSingleton<CollegeDbContextFactory>();
        services.AddSingleton<ICollegeDbContextFactory>(sp => sp.GetRequiredService<CollegeDbContextFactory>());
        services.AddSingleton<TenantMigrator>();
        services.AddSingleton<ITenantDatabaseManager, TenantDatabaseManager>();
        services.AddTenantMigration<InitialSchema>();

        // Security
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        return services;
    }

    public static IServiceCollection AddTenantMigration<T>(this IServiceCollection services)
        where T : class, ITenantMigration =>
        services.AddSingleton<ITenantMigration, T>();
}
