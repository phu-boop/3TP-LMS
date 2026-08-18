using Aig.Lms.Api.Authorization;
using Aig.Lms.Api.Extensions;
using Aig.Lms.BuildingBlocks.Contracts.Tenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace Aig.Lms.Api.Endpoints;

public static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/analytics")
            .WithTags("Analytics")
            .RequireAuthorization();

        group.MapGet("/summary", async (
            [FromQuery] Guid? tenantId,
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate,
            [FromQuery] string? provinceName,
            ICurrentTenant currentTenant,
            ClaimsPrincipal user,
            [FromServices] IAnalyticsRepository repository,
            CancellationToken ct) =>
        {
            if (fromDate == default || toDate == default || fromDate > toDate)
                return Results.BadRequest(new { error = "'fromDate' must be before or equal 'toDate' and both must be valid dates." });

            var utcFromDate = DateTime.SpecifyKind(fromDate, DateTimeKind.Utc);
            var utcToDate = DateTime.SpecifyKind(toDate, DateTimeKind.Utc).AddDays(1);

            var isSystemAdmin = user.IsInRole("SUPER_ADMIN") || user.IsInRole("LMS_ADMIN");
            Guid? targetTenantId = null;

            if (!isSystemAdmin)
            {
                targetTenantId = EndpointHelper.ResolveTenantId(currentTenant, user);
                if (!targetTenantId.HasValue)
                    return Results.Unauthorized();
            }
            else
            {
                targetTenantId = tenantId;
            }

            var summary = await repository.GetSummaryAsync(targetTenantId, utcFromDate, utcToDate, provinceName, ct);
            return Results.Ok(summary);
        })
        .WithName("GetAnalyticsSummary")
        .WithSummary("Get total learners, 1-year summary, and average study hours")
        .Produces<AnalyticsSummaryDto>(200)
        .ProducesProblem(401);

        group.MapGet("/regional", async (
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate,
            [FromQuery] string? provinceName,
            [FromQuery] Guid? tenantId,
            ICurrentTenant currentTenant,
            ClaimsPrincipal user,
            [FromServices] IAnalyticsRepository repository,
            CancellationToken ct) =>
        {
            if (fromDate == default || toDate == default || fromDate > toDate)
                return Results.BadRequest(new { error = "'fromDate' must be before or equal 'toDate' and both must be valid dates." });

            var utcFromDate = DateTime.SpecifyKind(fromDate, DateTimeKind.Utc);
            var utcToDate = DateTime.SpecifyKind(toDate, DateTimeKind.Utc).AddDays(1);

            var isSystemAdmin = user.IsInRole("SUPER_ADMIN") || user.IsInRole("LMS_ADMIN");
            Guid? targetTenantId = null;

            if (!isSystemAdmin)
            {
                targetTenantId = EndpointHelper.ResolveTenantId(currentTenant, user);
                if (!targetTenantId.HasValue)
                    return Results.Unauthorized();
            }
            else
            {
                targetTenantId = tenantId;
            }

            var regionalData = await repository.GetRegionalAnalyticsAsync(targetTenantId, utcFromDate, utcToDate, provinceName, ct);
            return Results.Ok(regionalData);
        })
        .WithName("GetRegionalAnalytics")
        .WithSummary("Get study time grouped by region")
        .Produces<IReadOnlyList<RegionalAnalyticsDto>>(200)
        .ProducesProblem(400)
        .ProducesProblem(401);

        group.MapGet("/top-schools", async (
            [FromQuery] Guid? tenantId,
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate,
            [FromQuery] string? provinceName,
            ICurrentTenant currentTenant,
            ClaimsPrincipal user,
            [FromServices] IAnalyticsRepository repository,
            CancellationToken ct) =>
        {
            if (fromDate == default || toDate == default || fromDate > toDate)
                return Results.BadRequest(new { error = "'fromDate' must be before or equal 'toDate' and both must be valid dates." });

            var utcFromDate = DateTime.SpecifyKind(fromDate, DateTimeKind.Utc);
            var utcToDate = DateTime.SpecifyKind(toDate, DateTimeKind.Utc).AddDays(1);

            var isSystemAdmin = user.IsInRole("SUPER_ADMIN") || user.IsInRole("LMS_ADMIN");
            Guid? targetTenantId = null;

            if (!isSystemAdmin)
            {
                targetTenantId = EndpointHelper.ResolveTenantId(currentTenant, user);
                if (!targetTenantId.HasValue)
                    return Results.Unauthorized();
            }
            else
            {
                targetTenantId = tenantId;
            }

            var topSchools = await repository.GetTopSchoolsAsync(targetTenantId, utcFromDate, utcToDate, provinceName, ct);
            return Results.Ok(topSchools);
        })
        .WithName("GetTopSchoolsAnalytics")
        .WithSummary("Get top schools by lesson count")
        .Produces<IReadOnlyList<TopSchoolAnalyticsDto>>(200)
        .ProducesProblem(401);

        group.MapGet("/top-tenants", async (
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate,
            [FromQuery] string? provinceName,
            ClaimsPrincipal user,
            [FromServices] IAnalyticsRepository repository,
            CancellationToken ct) =>
        {
            if (fromDate == default || toDate == default || fromDate > toDate)
                return Results.BadRequest(new { error = "'fromDate' must be before or equal 'toDate' and both must be valid dates." });

            var utcFromDate = DateTime.SpecifyKind(fromDate, DateTimeKind.Utc);
            var utcToDate = DateTime.SpecifyKind(toDate, DateTimeKind.Utc).AddDays(1);

            var isSystemAdmin = user.IsInRole("SUPER_ADMIN") || user.IsInRole("LMS_ADMIN");
            if (!isSystemAdmin)
                return Results.Forbid();

            var topTenants = await repository.GetTopTenantsAsync(null, utcFromDate, utcToDate, provinceName, ct);
            return Results.Ok(topTenants);
        })
        .WithName("GetTopTenantsAnalytics")
        .WithSummary("Get top tenants by lesson count")
        .Produces<IReadOnlyList<TopTenantAnalyticsDto>>(200)
        .ProducesProblem(401)
        .ProducesProblem(403);

        group.MapGet("/trend", async (
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate,
            [FromQuery] string? provinceName,
            [FromQuery] string groupBy,
            ICurrentTenant currentTenant,
            ClaimsPrincipal user,
            [FromServices] IAnalyticsRepository repository,
            CancellationToken ct) =>
        {
            if (fromDate == default || toDate == default || fromDate > toDate)
                return Results.BadRequest(new { error = "'fromDate' must be before or equal 'toDate' and both must be valid dates." });
            if (groupBy != "day" && groupBy != "week" && groupBy != "month")
                return Results.BadRequest(new { error = "'groupBy' must be 'day', 'week', or 'month'." });

            var utcFromDate = DateTime.SpecifyKind(fromDate, DateTimeKind.Utc);
            var utcToDate = DateTime.SpecifyKind(toDate, DateTimeKind.Utc).AddDays(1);

            var isSystemAdmin = user.IsInRole("SUPER_ADMIN") || user.IsInRole("LMS_ADMIN");
            Guid? targetTenantId = null;

            if (!isSystemAdmin)
            {
                targetTenantId = EndpointHelper.ResolveTenantId(currentTenant, user);
                if (!targetTenantId.HasValue)
                    return Results.Unauthorized();
            }

            var trend = await repository.GetTrendAsync(targetTenantId, utcFromDate, utcToDate, provinceName, groupBy, ct);
            return Results.Ok(trend);
        })
        .WithName("GetTrendAnalytics")
        .WithSummary("Get study trend (learners count and study hours) grouped by day/week/month")
        .Produces<IReadOnlyList<TrendAnalyticsDto>>(200)
        .ProducesProblem(400)
        .ProducesProblem(401);

        return app;
    }
}
