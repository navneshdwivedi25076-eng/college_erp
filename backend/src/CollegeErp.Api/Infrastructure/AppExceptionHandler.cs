using CollegeErp.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CollegeErp.Api.Infrastructure;

/// <summary>Translates application exceptions into RFC 7807 problem responses.</summary>
public sealed class AppExceptionHandler(IProblemDetailsService problems, ILogger<AppExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        ProblemDetails problem = exception switch
        {
            InvalidInputException v => new HttpValidationProblemDetails(v.Errors)
                { Status = 400, Title = v.Message },
            NotFoundException e => new ProblemDetails { Status = 404, Title = "Not found", Detail = e.Message },
            ConflictException e => new ProblemDetails { Status = 409, Title = "Conflict", Detail = e.Message },
            ForbiddenException e => new ProblemDetails { Status = 403, Title = "Forbidden", Detail = e.Message },
            UnauthorizedException e => new ProblemDetails { Status = 401, Title = "Unauthorized", Detail = e.Message },
            DbUpdateException => new ProblemDetails
            {
                Status = 409, Title = "Conflict",
                Detail = "The change conflicts with existing data (duplicate value or record still in use).",
            },
            _ => new ProblemDetails { Status = 500, Title = "An unexpected error occurred." },
        };
        if (problem.Status == 500) logger.LogError(exception, "Unhandled exception");
        else if (exception is DbUpdateException) logger.LogWarning(exception, "Database update conflict");

        context.Response.StatusCode = problem.Status!.Value;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context, ProblemDetails = problem, Exception = exception,
        });
    }
}
