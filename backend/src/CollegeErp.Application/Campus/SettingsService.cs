using System.Text.Json;
using CollegeErp.Application.Abstractions;
using CollegeErp.Application.Common;
using CollegeErp.Domain.Campus;
using Microsoft.EntityFrameworkCore;

namespace CollegeErp.Application.Campus;

public interface ISettingsService
{
    Task<IReadOnlyDictionary<string, JsonElement>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, JsonElement>> UpsertAsync(IDictionary<string, JsonElement> values, CancellationToken ct = default);
}

/// <summary>Key/value configuration so each college can customise behaviour without schema changes.</summary>
public sealed class SettingsService(ICollegeDbContext db) : ISettingsService
{
    public async Task<IReadOnlyDictionary<string, JsonElement>> GetAllAsync(CancellationToken ct = default) =>
        (await db.Settings.AsNoTracking().ToListAsync(ct))
        .ToDictionary(s => s.Key, s => JsonDocument.Parse(s.Value).RootElement.Clone());

    public async Task<IReadOnlyDictionary<string, JsonElement>> UpsertAsync(
        IDictionary<string, JsonElement> values, CancellationToken ct = default)
    {
        if (values.Keys.Any(k => string.IsNullOrWhiteSpace(k) || k.Length > 100))
            throw new InvalidInputException("key", "Setting keys must be 1-100 characters.");
        var keys = values.Keys.ToList();
        var existing = await db.Settings.Where(s => keys.Contains(s.Key)).ToDictionaryAsync(s => s.Key, ct);
        foreach (var (key, value) in values)
        {
            var json = value.GetRawText();
            if (existing.TryGetValue(key, out var setting)) setting.Value = json;
            else db.Settings.Add(new CollegeSetting { Key = key, Value = json });
        }
        await db.SaveChangesAsync(ct);
        return await GetAllAsync(ct);
    }
}
