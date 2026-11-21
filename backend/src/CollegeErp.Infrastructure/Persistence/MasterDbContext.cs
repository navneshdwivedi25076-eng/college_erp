using CollegeErp.Application.Abstractions;
using CollegeErp.Domain.Platform;
using Microsoft.EntityFrameworkCore;

namespace CollegeErp.Infrastructure.Persistence;

public sealed class MasterDbContext(DbContextOptions<MasterDbContext> options) : DbContext(options), IMasterDbContext
{
    public DbSet<SuperAdmin> SuperAdmins => Set<SuperAdmin>();
    public DbSet<College> Colleges => Set<College>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<SuperAdmin>(e =>
        {
            e.ToTable("super_admins");
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(255);
            e.Property(x => x.FullName).HasMaxLength(255);
        });
        b.Entity<College>(e =>
        {
            e.ToTable("colleges");
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.DatabaseName).IsUnique();
            e.Property(x => x.Code).HasMaxLength(40);
            e.Property(x => x.Name).HasMaxLength(255);
            e.Property(x => x.DatabaseName).HasMaxLength(100);
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.Plan).HasMaxLength(50);
            e.Ignore(x => x.IsActive);
        });
    }
}
