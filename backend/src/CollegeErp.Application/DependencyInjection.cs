using CollegeErp.Application.Campus;
using CollegeErp.Application.Campus.Academics;
using CollegeErp.Application.Campus.Crud;
using CollegeErp.Application.Campus.Finance;
using CollegeErp.Application.Campus.Identity;
using CollegeErp.Application.Campus.Portal;
using CollegeErp.Application.Platform;
using CollegeErp.Domain.Campus;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeErp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        // Platform (super admin) use cases
        services.AddScoped<IPlatformAuthService, PlatformAuthService>();
        services.AddScoped<ICollegeOnboardingService, CollegeOnboardingService>();
        services.AddScoped<ICollegeAdministrationService, CollegeAdministrationService>();
        services.AddScoped<ISuperAdminService, SuperAdminService>();

        // College (tenant) use cases
        services.AddScoped<ICollegeAuthService, CollegeAuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ReferenceGuard>();
        services.AddScoped<ICourseAccessPolicy, CourseAccessPolicy>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IMarksService, MarksService>();
        services.AddScoped<IFeeService, FeeService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IStudentPortalService, StudentPortalService>();

        // Generic CRUD + its extension points (filters and rules)
        services.AddScoped(typeof(ICrudService<,>), typeof(CrudService<,>));
        services.AddScoped(typeof(IQueryFilter<>), typeof(NoFilter<>));
        services.AddScoped<IQueryFilter<Department>, DepartmentFilter>();
        services.AddScoped<IQueryFilter<FacultyMember>, FacultyFilter>();
        services.AddScoped<IQueryFilter<Student>, StudentFilter>();
        services.AddScoped<IQueryFilter<Course>, CourseFilter>();
        services.AddScoped<IQueryFilter<Exam>, ExamFilter>();

        services.AddScoped<IEntityRule<Student>, StudentCapacityRule>();
        services.AddScoped<IEntityRule<Student>, StudentReferencesRule>();
        services.AddScoped<IEntityRule<FacultyMember>, FacultyReferencesRule>();
        services.AddScoped<IEntityRule<Course>, CourseReferencesRule>();
        services.AddScoped<IEntityRule<Exam>, ExamCourseAccessRule>();
        services.AddScoped<IDeleteRule<Exam>, ExamDeleteAccessRule>();
        services.AddScoped<IDeleteRule<Department>, DepartmentInUseRule>();
        return services;
    }
}
