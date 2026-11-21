using CollegeErp.Application.Platform;
using CollegeErp.Infrastructure;
using CollegeErp.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace CollegeErp.Api.Infrastructure;

public static class DatabaseInitializer
{
    /// <summary>Creates the master DB, seeds the first super admin and upgrades every college DB.</summary>
    public static async Task InitializeDatabasesAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var log = sp.GetRequiredService<ILogger<WebApplication>>();

        await sp.GetRequiredService<MasterDbContext>().Database.EnsureCreatedAsync();

        var bootstrap = sp.GetRequiredService<IOptions<BootstrapOptions>>().Value;
        await sp.GetRequiredService<ISuperAdminService>()
            .EnsureBootstrapAdminAsync(bootstrap.SuperAdminEmail, bootstrap.SuperAdminPassword);

        if (sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.MigrateTenantsOnStartup)
        {
            var report = await sp.GetRequiredService<ICollegeAdministrationService>().MigrateAllAsync();
            log.LogInformation("College databases checked: {Count} up to date, {Failed} failed",
                report.Upgraded, report.Failures.Count);
        }
    }
}
