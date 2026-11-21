using System.Text.RegularExpressions;
using CollegeErp.Application.Abstractions;
using CollegeErp.Domain.Common;
using CollegeErp.Infrastructure.Tenancy;

namespace CollegeErp.Api.Infrastructure;

/// <summary>
/// Resolves the college for every request under /api/t/{collegeCode}/... and binds
/// the request's database context to that college's own database.
/// </summary>
public sealed partial class TenantResolutionMiddleware(RequestDelegate next)
{
    [GeneratedRegex("^/api/t/(?<code>[A-Za-z0-9_]{3,40})(/|$)")]
    private static partial Regex TenantPath();

    public async Task InvokeAsync(HttpContext context, ICollegeDirectory directory, ITenantSetter tenant)
    {
        var match = TenantPath().Match(context.Request.Path.Value ?? "");
        if (!match.Success)
        {
            await next(context);
            return;
        }

        var college = await directory.FindByCodeAsync(match.Groups["code"].Value, context.RequestAborted);
        if (college is null)
        {
            await Results.Problem(statusCode: 404, title: "College not found").ExecuteAsync(context);
            return;
        }
        if (college.Status != CollegeStatus.Active)
        {
            await Results.Problem(statusCode: 403, title: "College suspended",
                detail: "This college's account is suspended. Contact the platform administrator.").ExecuteAsync(context);
            return;
        }

        tenant.Set(college);
        await next(context);
    }
}
