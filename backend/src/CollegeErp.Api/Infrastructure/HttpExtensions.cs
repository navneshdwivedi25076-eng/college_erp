using CollegeErp.Application.Common;

namespace CollegeErp.Api.Infrastructure;

public static class HttpExtensions
{
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase) { "skip", "take", "search" };

    /// <summary>Builds a <see cref="ListQuery"/> from ?skip=&amp;take=&amp;search= plus any other key as an exact filter.</summary>
    public static ListQuery ToListQuery(this HttpRequest request)
    {
        var q = request.Query;
        int.TryParse(q["skip"], out var skip);
        var take = int.TryParse(q["take"], out var t) ? t : 50;
        var filters = q.Where(kv => !Reserved.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        return new ListQuery(skip, take, q["search"].ToString() is { Length: > 0 } s ? s : null, filters);
    }
}
