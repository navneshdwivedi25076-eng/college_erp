using CollegeErp.Application.Abstractions;
using CollegeErp.Application.Common;
using CollegeErp.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace CollegeErp.Application.Campus.Crud;

/// <summary>Validates that foreign-key ids point to existing rows, with a friendly error.</summary>
public sealed class ReferenceGuard(ICollegeDbContext db)
{
    public async Task EnsureExistsAsync<TRef>(int? id, string field, CancellationToken ct) where TRef : Entity
    {
        if (id is null) return;
        if (!await db.Set<TRef>().AnyAsync(e => e.Id == id, ct))
            throw new InvalidInputException(field, $"{typeof(TRef).Name} {id} does not exist.");
    }
}
