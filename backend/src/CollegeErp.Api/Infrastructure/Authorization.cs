using System.Security.Claims;
using CollegeErp.Application.Abstractions;
using CollegeErp.Domain.Common;
using CollegeErp.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;

namespace CollegeErp.Api.Infrastructure;

public static class Policies
{
    public const string Platform = "platform";
    public const string AnyCollegeUser = "college:any";
    public const string CollegeAdmin = "college:admin";
    public const string Staff = "college:staff";
    public const string Academic = "college:academic";
    public const string Finance = "college:finance";
    public const string Student = "college:student";

    public static AuthorizationBuilder AddAppPolicies(this AuthorizationBuilder auth) => auth
        .AddPolicy(Platform, p => p.RequireAuthenticatedUser().RequireClaim(AppClaims.Scope, AppClaims.PlatformScope))
        .AddPolicy(AnyCollegeUser, p => p.AddRequirements(new CollegeAccessRequirement()))
        .AddPolicy(CollegeAdmin, p => p.AddRequirements(new CollegeAccessRequirement(Roles.CollegeAdmin)))
        .AddPolicy(Staff, p => p.AddRequirements(new CollegeAccessRequirement(Roles.CollegeAdmin, Roles.Faculty, Roles.Accountant)))
        .AddPolicy(Academic, p => p.AddRequirements(new CollegeAccessRequirement(Roles.CollegeAdmin, Roles.Faculty)))
        .AddPolicy(Finance, p => p.AddRequirements(new CollegeAccessRequirement(Roles.CollegeAdmin, Roles.Accountant)))
        .AddPolicy(Student, p => p.AddRequirements(new CollegeAccessRequirement(Roles.Student)));
}

/// <summary>User must hold a token issued by THIS college and (optionally) one of the roles.</summary>
public sealed class CollegeAccessRequirement(params string[] roles) : IAuthorizationRequirement
{
    public IReadOnlyList<string> Roles { get; } = roles;
}

public sealed class CollegeAccessHandler(ICurrentTenant tenant) : AuthorizationHandler<CollegeAccessRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CollegeAccessRequirement req)
    {
        var user = context.User;
        var sameCollege = tenant.IsResolved
            && user.FindFirstValue(AppClaims.Scope) == AppClaims.CollegeScope
            && string.Equals(user.FindFirstValue(AppClaims.College), tenant.College.Code, StringComparison.Ordinal);
        var roleOk = req.Roles.Count == 0 || req.Roles.Any(user.IsInRole);
        if (sameCollege && roleOk) context.Succeed(req);
        return Task.CompletedTask;
    }
}

/// <summary>Exposes the authenticated college user from the JWT claims.</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal User => accessor.HttpContext?.User ?? new ClaimsPrincipal();

    public int UserId => int.TryParse(User.FindFirstValue("sub"), out var id) ? id : 0;
    public string Role => User.FindFirstValue(ClaimTypes.Role) ?? "";
    public int? StudentId => int.TryParse(User.FindFirstValue(AppClaims.StudentId), out var v) ? v : null;
    public int? FacultyId => int.TryParse(User.FindFirstValue(AppClaims.FacultyId), out var v) ? v : null;
}
