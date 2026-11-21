using System.Security.Claims;
using System.Text;
using CollegeErp.Api.Endpoints;
using CollegeErp.Api.Infrastructure;
using CollegeErp.Application;
using CollegeErp.Application.Abstractions;
using CollegeErp.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---- Layers (composition root) ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---- Web concerns ----
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddValidation();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never);

var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
if (Encoding.UTF8.GetByteCount(jwt.Secret) < 32)
    throw new InvalidOperationException("Jwt:Secret must be at least 32 bytes. Set it in appsettings or the Jwt__Secret env var.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = jwt.Issuer,
        ValidAudience = jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = "email",
        ClockSkew = TimeSpan.FromMinutes(1),
    };
});
builder.Services.AddScoped<IAuthorizationHandler, CollegeAccessHandler>();
builder.Services.AddAuthorizationBuilder().AddAppPolicies();

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

await app.InitializeDatabasesAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.UseMiddleware<TenantResolutionMiddleware>();   // binds /api/t/{code}/... to that college's DB
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" })).WithTags("Health");
app.MapPlatformEndpoints();
app.MapCollegeEndpoints();

// Serve the built React app (copied into wwwroot) when present.
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
