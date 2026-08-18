using System.Text.Json;
using Aig.Lms.Api;
using Aig.Lms.Api.Authorization;
using Aig.Lms.Api.Extensions;
using Aig.Lms.Api.Middleware;
using Aig.Lms.Modules.AuditLogs.Infrastructure;
using Aig.Lms.Modules.Identity.Infrastructure;
using Aig.Lms.Modules.Tenancy.Infrastructure;
using Aig.Lms.Modules.Authorization.Infrastructure;
using Aig.Lms.Modules.Users.Infrastructure;
using Aig.Lms.Modules.Catalog.Infrastructure;
using Aig.Lms.Modules.Schools.Infrastructure;
using Aig.Lms.Modules.ContentManagement.Infrastructure;
using Aig.Lms.Modules.ContentManagement.Application.Content;
using Aig.Lms.Modules.Tenancy.Infrastructure.Services;
using Aig.Lms.BuildingBlocks.Infrastructure.Storage;
using Aig.Lms.BuildingBlocks.Infrastructure.Emails;
using Aig.Lms.Workers.BackgroundJobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Aig.Lms.Modules.Reports.Infrastructure;
using Aig.Lms.Modules.Tickets.Infrastructure;
using Aig.Lms.Modules.Utils;
using Aig.Lms.Api.MediaProcessing;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Options;
using Npgsql;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// CORS — allow UI and services to call the API
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()?
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim())
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray() ?? [];

var tenantBaseDomains = builder.Configuration
    .GetSection("Tenancy:TenantBaseDomains")
    .Get<string[]>()?
    .Where(domain => !string.IsNullOrWhiteSpace(domain))
    .Select(domain => domain.Trim().Trim('.').ToLowerInvariant())
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowConfiguredOrigins", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            return;
        }

        policy.SetIsOriginAllowed(origin => IsAllowedOrigin(origin, allowedOrigins, tenantBaseDomains))
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// OpenAPI / Scalar
builder.Services.AddOpenApiDocs();

// JWT Authentication + Authorization
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddPermissionAuthorization();

// Identity module (login, token)
builder.Services.AddIdentityModule(builder.Configuration);

// Tenancy module (admin tenants, domain resolution)
builder.Services.AddTenancyModule(builder.Configuration);
builder.Services.AddMemoryCache();

// Users module (user management)
builder.Services.AddUsersModule(builder.Configuration);

// AuditLogs module (immutable audit trail)
builder.Services.AddAuditLogsModule(builder.Configuration);

// Authorization module (roles, role assignments)
builder.Services.AddAuthorizationModule(builder.Configuration);

// Catalog module (master data)
builder.Services.AddCatalogModule(builder.Configuration);

// Schools module (school management)
builder.Services.AddSchoolsModule(builder.Configuration);

// ContentManagement module (tenant curriculum tree)
builder.Services.AddContentManagementModule(builder.Configuration);
builder.Services.AddScoped<Aig.Lms.Api.Endpoints.IAnalyticsRepository, Aig.Lms.Api.Endpoints.AnalyticsRepository>();

// Object storage foundation (MinIO signer + URL builder)
builder.Services.AddObjectStorageFoundation(builder.Configuration);

// Media processing pipeline (HLS transcode worker + Redis queue)
builder.Services.Configure<MediaProcessingOptions>(builder.Configuration.GetSection("MediaProcessing"));
builder.Services.AddHttpClient("MediaProcessing");
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var options = sp.GetRequiredService<IOptions<MediaProcessingOptions>>().Value;
    var redisConnection = builder.Configuration.GetConnectionString("Redis");
    var connection = string.IsNullOrWhiteSpace(redisConnection)
        ? options.RedisConnection
        : redisConnection;

    return ConnectionMultiplexer.Connect(connection);
});
builder.Services.AddSingleton<IVideoTranscodeJobQueue, RedisVideoTranscodeJobQueue>();
builder.Services.AddSingleton<IMediaProcessingDispatcher, MediaProcessingDispatcher>();
builder.Services.AddHostedService<BackgroundVideoTranscodeWorker>();

// Reports module (report management)
builder.Services.AddReportsModule(builder.Configuration);

// Tickets module (ticket management)
builder.Services.AddTicketsModule(builder.Configuration);

// Utils module
builder.Services.AddUtils();

// Email foundation (Outbox + SMTP sender + Template engine + Worker)
builder.Services.AddEmailFoundation(builder.Configuration);
builder.Services.AddEmailBackgroundJobs();

// Health checks
builder.Services.AddHealthChecks();

// HTTP logging for production traceability without request/response bodies.
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestMethod
                            | HttpLoggingFields.RequestPath
                            | HttpLoggingFields.ResponseStatusCode
                            | HttpLoggingFields.Duration;
    options.CombineLogs = true;
});

var app = builder.Build();

app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
{
    var feature = ctx.Features.Get<IExceptionHandlerFeature>();
    var ex = feature?.Error;

    var (statusCode, message) = ex switch
    {
        BadHttpRequestException { InnerException: JsonException jsonEx }
            => (400, BuildJsonErrorMessage(jsonEx)),
        BadHttpRequestException badReq
            => (badReq.StatusCode, badReq.Message),
        ArgumentException argEx
            => (400, argEx.Message),
        InvalidOperationException invalidOp
            => (409, invalidOp.Message),
        PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
            => (409, "Duplicate data: one or more unique fields already exist."),
        PostgresException { SqlState: PostgresErrorCodes.NotNullViolation }
            => (400, "Missing required field(s). Please check request payload."),
        PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation }
            => (400, "Invalid reference data (schoolId, tenantId, roleId, or related entity not found)."),
        Aig.Lms.BuildingBlocks.Domain.Exceptions.DomainException domainEx
            => (400, domainEx.Message),
        _ => (500, "An unexpected error occurred.")
    };

    ctx.Response.StatusCode = statusCode;
    ctx.Response.ContentType = "application/json";
    await ctx.Response.WriteAsJsonAsync(new { error = message });
}));

app.UseHttpLogging();
app.UseCors("AllowConfiguredOrigins");
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseSessionRevocation();
app.UseTenantResolution();
app.UseAuthorization();

app.MapAllEndpoints();

app.Run();

static string BuildJsonErrorMessage(JsonException jsonEx)
{
    // Extract the field name from the JSON path (e.g. "$.parentId" → "parentId")
    var path = jsonEx.Path ?? "";
    var field = path.StartsWith("$.") ? path[2..] : path;

    // Surface a specific message for common format errors
    var inner = jsonEx.InnerException?.Message ?? jsonEx.Message;
    if (inner.Contains("Guid", StringComparison.OrdinalIgnoreCase) ||
        inner.Contains("supported Guid format", StringComparison.OrdinalIgnoreCase))
    {
        return string.IsNullOrEmpty(field)
            ? "Invalid value: expected a valid UUID (e.g. '3fa85f64-5717-4562-b3fc-2c963f66afa6')."
            : $"Field '{field}' must be a valid UUID (e.g. '3fa85f64-5717-4562-b3fc-2c963f66afa6').";
    }

    return string.IsNullOrEmpty(field)
        ? $"Invalid request body: {jsonEx.Message}"
        : $"Field '{field}' has an invalid value: {jsonEx.Message}";
}

static bool IsAllowedOrigin(string origin, IReadOnlyCollection<string> explicitOrigins, IReadOnlyCollection<string> tenantBaseDomains)
{
    if (string.IsNullOrWhiteSpace(origin))
        return false;

    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        return false;

    var normalizedOrigin = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
    if (explicitOrigins.Any(o => string.Equals(o.TrimEnd('/'), normalizedOrigin, StringComparison.OrdinalIgnoreCase)))
        return true;

    var host = uri.Host.Trim('.').ToLowerInvariant();
    return tenantBaseDomains.Any(baseDomain =>
        host.Equals(baseDomain, StringComparison.OrdinalIgnoreCase)
        || host.EndsWith($".{baseDomain}", StringComparison.OrdinalIgnoreCase));
}

public partial class Program;
