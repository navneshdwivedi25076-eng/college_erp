namespace CollegeErp.Application.Common;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Skip, int Take);

/// <summary>Generic list query: paging + free-text search + exact-match filters (e.g. departmentId=3).</summary>
public sealed record ListQuery(
    int Skip = 0,
    int Take = 50,
    string? Search = null,
    IReadOnlyDictionary<string, string>? Filters = null)
{
    public int SafeTake => Math.Clamp(Take, 1, 500);
    public int SafeSkip => Math.Max(Skip, 0);

    public int? IntFilter(string name) =>
        Filters != null && Filters.TryGetValue(name, out var v) && int.TryParse(v, out var i) ? i : null;

    public string? StringFilter(string name) =>
        Filters != null && Filters.TryGetValue(name, out var v) && !string.IsNullOrWhiteSpace(v) ? v : null;
}
