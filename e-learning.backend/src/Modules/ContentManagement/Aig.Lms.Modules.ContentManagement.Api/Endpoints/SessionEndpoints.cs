using Aig.Lms.BuildingBlocks.Contracts.Tenancy;
using Aig.Lms.Modules.ContentManagement.Application.Content;
using Aig.Lms.Modules.ContentManagement.Application.Session;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using System.Text.Json;

namespace Aig.Lms.Modules.ContentManagement.Api.Endpoints;

public sealed record StartSessionRequest(Guid LearningSessionId, Guid ContentItemId, JsonDocument? DeviceInfo);
public sealed record HeartbeatRequest(Guid LearningSessionId);
public sealed record EndSessionRequest(Guid LearningSessionId);

public static class SessionEndpoints
{
    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/content/session")
            .WithTags("LearningSession")
            .RequireAuthorization();

        group.MapPost("/start", async (
            ICurrentTenant currentTenant,
            ClaimsPrincipal user,
            [FromBody] StartSessionRequest body,
            [FromServices] StartSessionCommandHandler handler,
            CancellationToken ct) =>
        {
            if (!currentTenant.TenantId.HasValue)
                return Results.Unauthorized();

            var userId = GetUserId(user);
            if (!userId.HasValue)
                return Results.Unauthorized();

            var schoolId = GetSchoolId(user);
            var sessionId = GetSessionId(user) ?? Guid.NewGuid(); // Fallback if not in claims

            var result = await handler.HandleAsync(new StartSessionCommand(
                currentTenant.TenantId.Value,
                userId.Value,
                schoolId,
                body.LearningSessionId,
                sessionId,
                body.ContentItemId,
                body.DeviceInfo,
                null // IP address could be extracted from HttpContext if needed
            ), ct);

            return Results.Created($"/api/content/session/{result.Id}", result);
        })
        .WithName("StartLearningSession")
        .WithSummary("Start a new learning session for a content item")
        .Produces<LearningSessionDto>(201)
        .ProducesProblem(401);

        group.MapPut("/heartbeat", async (
            ICurrentTenant currentTenant,
            ClaimsPrincipal user,
            [FromBody] HeartbeatRequest body,
            [FromServices] HeartbeatCommandHandler handler,
            CancellationToken ct) =>
        {
            if (!currentTenant.TenantId.HasValue)
                return Results.Unauthorized();

            var userId = GetUserId(user);
            if (!userId.HasValue)
                return Results.Unauthorized();

            await handler.HandleAsync(new HeartbeatCommand(
                currentTenant.TenantId.Value,
                userId.Value,
                body.LearningSessionId
            ), ct);

            return Results.Ok(new { updatedAt = DateTime.UtcNow });
        })
        .WithName("UpdateLearningSessionHeartbeat")
        .WithSummary("Update heartbeat and inactivity for a learning session")
        .Produces(200)
        .ProducesProblem(401);

        group.MapPut("/end", async (
            ICurrentTenant currentTenant,
            ClaimsPrincipal user,
            [FromBody] EndSessionRequest body,
            [FromServices] EndSessionCommandHandler handler,
            CancellationToken ct) =>
        {
            if (!currentTenant.TenantId.HasValue)
                return Results.Unauthorized();

            var userId = GetUserId(user);
            if (!userId.HasValue)
                return Results.Unauthorized();

            var result = await handler.HandleAsync(new EndSessionCommand(
                currentTenant.TenantId.Value,
                userId.Value,
                body.LearningSessionId
            ), ct);

            return Results.Ok(result);
        })
        .WithName("EndLearningSession")
        .WithSummary("End a learning session")
        .Produces<SessionSummaryDto>(200)
        .ProducesProblem(401);

        group.MapGet("/report", async (
            ICurrentTenant currentTenant,
            ClaimsPrincipal user,
            [FromServices] GetSessionReportQueryHandler handler,
            [FromQuery] Guid? userId,
            [FromQuery] Guid? schoolId,
            [FromQuery] Guid? contentItemId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default) =>
        {
            if (!currentTenant.TenantId.HasValue)
                return Results.Unauthorized();

            // Ensure user has admin permissions to view reports
            if (!user.HasClaim("Permission", "REPORT_VIEW"))
                return Results.Forbid();

            var result = await handler.HandleAsync(new SessionReportQuery(
                currentTenant.TenantId.Value,
                userId,
                schoolId,
                contentItemId,
                from,
                to,
                page,
                pageSize
            ), ct);

            return Results.Ok(result);
        })
        .WithName("GetLearningSessionReport")
        .WithSummary("Get aggregated learning session report")
        .Produces<PagedResult<SessionReportDto>>(200)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .RequireAuthorization("Permission:REPORT_VIEW");

        return app;
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
        return claim != null && Guid.TryParse(claim.Value, out var id) ? id : null;
    }

    private static Guid? GetSchoolId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst("school_id") ?? user.FindFirst("schoolId");
        return claim != null && Guid.TryParse(claim.Value, out var id) ? id : null;
    }

    private static Guid? GetSessionId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst("session_id") ?? user.FindFirst("sid");
        return claim != null && Guid.TryParse(claim.Value, out var id) ? id : null;
    }
}
