using System.Text.Json;
using CollegeErp.Api.Infrastructure;
using CollegeErp.Application.Campus;
using CollegeErp.Application.Campus.Academics;
using CollegeErp.Application.Campus.Finance;
using CollegeErp.Application.Campus.Identity;
using CollegeErp.Application.Campus.Portal;
using CollegeErp.Application.Platform;
using CollegeErp.Domain.Campus;

namespace CollegeErp.Api.Endpoints;

/// <summary>
/// Everything under /api/t/{collegeCode}. By the time these handlers run, the
/// TenantResolutionMiddleware has bound the request to that college's database.
/// </summary>
public static class CollegeEndpoints
{
    public static void MapCollegeEndpoints(this IEndpointRouteBuilder app)
    {
        var college = app.MapGroup("/api/t/{collegeCode}");
        MapAuth(college);
        MapAdministration(college);
        MapAcademics(college);
        MapFinance(college);
        MapStudentPortal(college);
    }

    private static void MapAuth(RouteGroupBuilder college)
    {
        var auth = college.MapGroup("/auth").WithTags("College auth");
        auth.MapPost("/login", (LoginRequest r, ICollegeAuthService svc, CancellationToken ct) =>
            svc.LoginAsync(r, ct)).AllowAnonymous();
        auth.MapGet("/me", (ICollegeAuthService svc, CancellationToken ct) => svc.MeAsync(ct))
            .RequireAuthorization(Policies.AnyCollegeUser);
    }

    private static void MapAdministration(RouteGroupBuilder college)
    {
        college.MapGet("/dashboard", (IDashboardService svc, CancellationToken ct) => svc.GetAsync(ct))
            .WithTags("Dashboard").RequireAuthorization(Policies.Staff);

        var users = college.MapGroup("/users").WithTags("Users").RequireAuthorization(Policies.CollegeAdmin);
        users.MapGet("", (HttpRequest req, IUserService svc, CancellationToken ct) => svc.ListAsync(req.ToListQuery(), ct));
        users.MapPost("", async (CreateUserRequest r, IUserService svc, CancellationToken ct) =>
            Results.Created("users", await svc.CreateAsync(r, ct)));
        users.MapPut("/{id:int}", (int id, UpdateUserRequest r, IUserService svc, CancellationToken ct) =>
            svc.UpdateAsync(id, r, ct));
        users.MapDelete("/{id:int}", async (int id, IUserService svc, CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        });

        var settings = college.MapGroup("/settings").WithTags("Settings");
        settings.MapGet("", (ISettingsService svc, CancellationToken ct) => svc.GetAllAsync(ct))
            .RequireAuthorization(Policies.AnyCollegeUser);
        settings.MapPut("", (Dictionary<string, JsonElement> values, ISettingsService svc, CancellationToken ct) =>
            svc.UpsertAsync(values, ct)).RequireAuthorization(Policies.CollegeAdmin);
    }

    private static void MapAcademics(RouteGroupBuilder college)
    {
        college.MapCrud<Department, DepartmentWrite>("/departments", Policies.AnyCollegeUser, Policies.CollegeAdmin);
        college.MapCrud<FacultyMember, FacultyWrite>("/faculty", Policies.Staff, Policies.CollegeAdmin);
        college.MapCrud<Student, StudentWrite>("/students", Policies.Staff, Policies.CollegeAdmin);
        college.MapCrud<Course, CourseWrite>("/courses", Policies.AnyCollegeUser, Policies.CollegeAdmin);
        var exams = college.MapCrud<Exam, ExamWrite>("/exams", Policies.Staff, Policies.Academic);

        exams.MapPost("/{id:int}/marks", (int id, RecordMarksRequest r, IMarksService svc, CancellationToken ct) =>
            svc.RecordAsync(id, r, ct)).RequireAuthorization(Policies.Academic);
        college.MapGet("/marks", (int? examId, int? studentId, IMarksService svc, CancellationToken ct) =>
            svc.ListAsync(examId, studentId, ct)).WithTags("exams").RequireAuthorization(Policies.Staff);

        var enrollments = college.MapGroup("/enrollments").WithTags("Enrollments");
        enrollments.MapGet("", (int? courseId, int? studentId, string? academicYear, IEnrollmentService svc, CancellationToken ct) =>
            svc.ListAsync(courseId, studentId, academicYear, ct)).RequireAuthorization(Policies.Staff);
        enrollments.MapPost("", async (EnrollRequest r, IEnrollmentService svc, CancellationToken ct) =>
            Results.Ok(new { added = await svc.EnrollAsync(r, ct) })).RequireAuthorization(Policies.CollegeAdmin);
        enrollments.MapDelete("/{id:int}", async (int id, IEnrollmentService svc, CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        }).RequireAuthorization(Policies.CollegeAdmin);

        var attendance = college.MapGroup("/attendance").WithTags("Attendance");
        attendance.MapPost("", async (MarkAttendanceRequest r, IAttendanceService svc, CancellationToken ct) =>
            Results.Ok(new { saved = await svc.MarkAsync(r, ct) })).RequireAuthorization(Policies.Academic);
        attendance.MapGet("", (int? courseId, int? studentId, DateOnly? date, IAttendanceService svc, CancellationToken ct) =>
            svc.ListAsync(courseId, studentId, date, ct)).RequireAuthorization(Policies.Staff);
        attendance.MapGet("/summary", (int courseId, IAttendanceService svc, CancellationToken ct) =>
            svc.SummaryAsync(courseId, null, ct)).RequireAuthorization(Policies.Staff);
    }

    private static void MapFinance(RouteGroupBuilder college)
    {
        var fees = college.MapGroup("/fees/invoices").WithTags("Fees").RequireAuthorization(Policies.Finance);
        fees.MapGet("", (HttpRequest req, IFeeService svc, CancellationToken ct) => svc.ListInvoicesAsync(req.ToListQuery(), ct));
        fees.MapPost("", async (CreateInvoiceRequest r, IFeeService svc, CancellationToken ct) =>
            Results.Ok(await svc.CreateInvoicesAsync(r, ct)));
        fees.MapDelete("/{id:int}", async (int id, IFeeService svc, CancellationToken ct) =>
        {
            await svc.DeleteInvoiceAsync(id, ct);
            return Results.NoContent();
        });
        fees.MapGet("/{id:int}/payments", (int id, IFeeService svc, CancellationToken ct) => svc.ListPaymentsAsync(id, ct));
        fees.MapPost("/{id:int}/payments", (int id, RecordPaymentRequest r, IFeeService svc, CancellationToken ct) =>
            svc.RecordPaymentAsync(id, r, ct));
    }

    private static void MapStudentPortal(RouteGroupBuilder college)
    {
        var me = college.MapGroup("/me").WithTags("Student portal").RequireAuthorization(Policies.Student);
        me.MapGet("/profile", (IStudentPortalService svc, CancellationToken ct) => svc.ProfileAsync(ct));
        me.MapGet("/courses", (IStudentPortalService svc, CancellationToken ct) => svc.CoursesAsync(ct));
        me.MapGet("/marks", (IStudentPortalService svc, CancellationToken ct) => svc.MarksAsync(ct));
        me.MapGet("/invoices", (IStudentPortalService svc, CancellationToken ct) => svc.InvoicesAsync(ct));
    }
}
