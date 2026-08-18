using Aig.Lms.BuildingBlocks.Contracts.Tenancy;
using Aig.Lms.Modules.Schools.Application;
using Aig.Lms.Modules.Schools.Domain;
using ClosedXML.Excel;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Aig.Lms.Modules.Schools.Api.Endpoints;

public static class SchoolsEndpoints
{
    public static IEndpointRouteBuilder MapSchoolsEndpoints(this IEndpointRouteBuilder app)
    {
        MapSchoolCrud(app);
        MapSubscriptionEndpoints(app);
        return app;
    }

    // ── School CRUD ───────────────────────────────────────────────────────────

    private static void MapSchoolCrud(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/schools")
            .WithTags("Schools")
            .RequireAuthorization();

        // GET /api/admin/schools
        group.MapGet("/", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            [FromQuery] string? status,
            [FromQuery] string? search,
            [FromQuery] string? province,
            [FromQuery] string? district,
            [FromQuery] bool? filterByCurrentTenant,
            [FromServices] ISchoolRepository repository,
            [FromServices] ICurrentTenant currentTenant,
            CancellationToken ct) =>
        {
            var clampedPage = page < 1 ? 1 : page;
            var clampedSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;

            var tenantIdFilter = (filterByCurrentTenant ?? false) ? currentTenant.TenantId : null;

            var items = await repository.ListAsync(clampedPage, clampedSize, status, search, province, district, tenantIdFilter, ct);
            var total = await repository.CountAsync(status, search, province, district, tenantIdFilter, ct);

            return Results.Ok(new SchoolListResult(items, total, clampedPage, clampedSize));
        })
        .WithName("ListSchools")
        .WithSummary("List schools with optional status/search/province/district filter")
        .Produces<SchoolListResult>()
        .ProducesProblem(401)
        .RequireAuthorization("Permission:SCHOOLS_VIEW");

        // GET /api/admin/schools/export — Export to Excel (.xlsx)
        group.MapGet("/export", async (
            [FromQuery] string? status,
            [FromQuery] string? search,
            [FromQuery] string? province,
            [FromQuery] string? district,
            [FromServices] ISchoolRepository repository,
            CancellationToken ct) =>
        {
            var schools = await repository.ListAllAsync(status, search, province, district, ct);

            if (schools.Count > 10000)
                return Results.BadRequest(new { error = "Dữ liệu quá lớn (hơn 10.000 bản ghi). Vui lòng lọc thêm điều kiện." });

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Danh sách trường học");

            // Header
            var headers = new[] {
                "STT", "Mã trường", "Tên trường", "Mã số thuế",
                "Tỉnh/Thành phố", "Phường/Xã", "Địa chỉ chi tiết",
                "Người liên hệ", "Email", "Số điện thoại",
                "Trạng thái"
            };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Data rows
            int row = 2;
            foreach (var s in schools)
            {
                ws.Cell(row, 1).Value = row - 1;
                ws.Cell(row, 2).Value = s.Code;
                ws.Cell(row, 3).Value = s.Name;

                ws.Cell(row, 4).Value = s.TaxId ?? "";
                ws.Cell(row, 4).Style.NumberFormat.Format = "@";

                ws.Cell(row, 5).Value = s.Province ?? "";
                ws.Cell(row, 6).Value = s.District ?? "";
                ws.Cell(row, 7).Value = s.Address ?? "";
                ws.Cell(row, 8).Value = s.ContactName ?? "";
                ws.Cell(row, 9).Value = s.ContactEmail ?? "";

                ws.Cell(row, 10).Value = s.ContactPhone ?? "";
                ws.Cell(row, 10).Style.NumberFormat.Format = "@";

                ws.Cell(row, 11).Value = s.Status == "ACTIVE" ? "Hoạt động" : "Ngưng";

                for (int i = 1; i <= headers.Length; i++)
                    ws.Cell(row, i).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var bytes = stream.ToArray();
            return Results.File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "danh-sach-truong-hoc.xlsx");
        })
        .WithName("ExportSchools")
        .WithSummary("Export schools to Excel")
        .Produces(StatusCodes.Status200OK, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .RequireAuthorization("Permission:SCHOOLS_VIEW");

        // GET /api/admin/schools/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] ISchoolRepository repository,
            CancellationToken ct) =>
        {
            var school = await repository.GetByIdAsync(id, ct);
            return school is null ? Results.NotFound() : Results.Ok(school);
        })
        .WithName("GetSchoolById")
        .WithSummary("Get a school by ID")
        .Produces<School>()
        .ProducesProblem(404)
        .ProducesProblem(401)
        .RequireAuthorization("Permission:SCHOOLS_VIEW");

        // POST /api/admin/schools
        group.MapPost("/", async (
            [FromBody] CreateSchoolRequest body,
            [FromServices] CreateSchoolCommandHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var command = new CreateSchoolCommand(
                    body.Code, body.Name, body.TaxId,
                    body.Province, body.District, body.Address,
                    body.ContactName, body.ContactEmail, body.ContactPhone,
                    body.ContractStartDate, body.ContractEndDate);

                var result = await handler.HandleAsync(command, ct);
                return Results.Created($"/api/admin/schools/{result.SchoolId}", result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        })
        .WithName("CreateSchool")
        .WithSummary("Create a new school")
        .Produces<CreateSchoolResult>(201)
        .ProducesProblem(409)
        .ProducesProblem(401)
        .RequireAuthorization("Permission:SCHOOLS_CREATE");

        // PUT /api/admin/schools/{id}
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateSchoolRequest body,
            [FromServices] UpdateSchoolCommandHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var command = new UpdateSchoolCommand(
                    id, body.Name, body.TaxId,
                    body.Province, body.District, body.Address,
                    body.ContactName, body.ContactEmail, body.ContactPhone,
                    body.ContractStartDate, body.ContractEndDate);

                var result = await handler.HandleAsync(command, ct);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("UpdateSchool")
        .WithSummary("Update school information")
        .Produces<UpdateSchoolResult>()
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(401)
        .RequireAuthorization("Permission:SCHOOLS_UPDATE");

        // PATCH /api/admin/schools/{id}/status
        group.MapPatch("/{id:guid}/status", async (
            Guid id,
            [FromBody] ChangeSchoolStatusRequest body,
            [FromServices] ISchoolRepository repository,
            CancellationToken ct) =>
        {
            var updated = await repository.ChangeStatusAsync(id, body.Status, ct);
            return updated ? Results.NoContent() : Results.NotFound();
        })
        .WithName("ChangeSchoolStatus")
        .WithSummary("Change school status: ACTIVE | INACTIVE | LOCKED")
        .Produces(204)
        .ProducesProblem(404)
        .ProducesProblem(401)
        .RequireAuthorization("Permission:SCHOOLS_CHANGE_STATUS");
    }

    // ── Subscriptions ─────────────────────────────────────────────────────────

    private static void MapSubscriptionEndpoints(IEndpointRouteBuilder app)
    {
        // GET /api/admin/subscriptions
        // LMS_ADMIN: can see all subscriptions and filter by tenant/school.
        // TENANT_ADMIN: can only see subscriptions for tenant(s) where they have TENANT_ADMIN role.
        app.MapGet("/api/admin/subscriptions", async (
            [FromQuery] Guid? tenantId,
            [FromQuery] Guid? schoolId,
            [FromQuery] string? status,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            [FromServices] ISchoolRepository repository,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var clampedPage = page < 1 ? 1 : page;
            var clampedSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;
            var normalizedStatus = string.IsNullOrWhiteSpace(status)
                ? null
                : status.Trim().ToUpperInvariant();

            var roleClaims = httpContext.User.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (roleClaims.Contains("LMS_ADMIN"))
            {
                var total = await repository.CountSubscriptionsAsync(schoolId, tenantId, normalizedStatus, null, ct);
                var fullItems = await repository.ListSubscriptionsAsync(schoolId, tenantId, normalizedStatus, null, clampedPage, clampedSize, ct);
                return Results.Ok(new SubscriptionListResult(fullItems, total, clampedPage, clampedSize));
            }

            if (!roleClaims.Contains("TENANT_ADMIN"))
                return Results.Forbid();

            var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? httpContext.User.FindFirstValue("sub");

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Results.Forbid();

            var allowedTenantIds = await repository.GetTenantAdminTenantIdsAsync(userId, ct);

            if (allowedTenantIds.Count == 0)
                return Results.Ok(new SubscriptionListResult(Array.Empty<SubscriptionDto>(), 0, clampedPage, clampedSize));

            if (tenantId.HasValue && !allowedTenantIds.Contains(tenantId.Value))
                return Results.Forbid();

            var scopedTotal = await repository.CountSubscriptionsAsync(schoolId, tenantId, normalizedStatus, allowedTenantIds, ct);
            var scopedItems = await repository.ListSubscriptionsAsync(schoolId, tenantId, normalizedStatus, allowedTenantIds, clampedPage, clampedSize, ct);
            return Results.Ok(new SubscriptionListResult(scopedItems, scopedTotal, clampedPage, clampedSize));
        })
        .WithTags("Schools")
        .WithName("ListSubscriptionsByScope")
        .WithSummary("List subscriptions by scope with optional tenantId, schoolId, status and pagination filters")
        .Produces<SubscriptionListResult>()
        .ProducesProblem(401)
        .ProducesProblem(403)
        .RequireAuthorization("Permission:SCHOOLS_VIEW");

        var group = app.MapGroup("/api/admin/schools/{schoolId:guid}/subscriptions")
            .WithTags("Schools")
            .RequireAuthorization();

        // GET /api/admin/schools/{schoolId}/subscriptions
        group.MapGet("/", async (
            Guid schoolId,
            [FromServices] ISchoolRepository repository,
            CancellationToken ct) =>
        {
            var items = await repository.ListSubscriptionsAsync(schoolId, ct);
            return Results.Ok(items);
        })
        .WithName("ListSubscriptions")
        .WithSummary("List all tenant subscriptions for a school")
        .Produces<IReadOnlyList<SubscriptionDto>>()
        .ProducesProblem(401)
        .RequireAuthorization("Permission:SCHOOLS_VIEW");

        // POST /api/admin/schools/{schoolId}/subscriptions
        // Accepts a list of TenantIds — creates one row per tenant with the same contract config.
        group.MapPost("/", async (
            Guid schoolId,
            [FromBody] CreateSubscriptionRequest body,
            [FromServices] CreateSubscriptionsBulkCommandHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var command = new CreateSubscriptionsBulkCommand(
                    schoolId, body.TenantIds,
                    body.ContractStart, body.ContractEnd,
                    body.MaxConcurrentSessions, body.LoginPolicy, body.EnforceExpiry);

                var result = await handler.HandleAsync(command, ct);

                if (result.Created.Count == 0)
                    return Results.Conflict(new { error = "All selected tenants already have active subscriptions.", skipped = result.SkippedDuplicates });

                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        })
        .WithName("CreateSubscriptions")
        .WithSummary("Create tenant subscriptions for a school (one row per selected tenant)")
        .Produces<CreateSubscriptionsBulkResult>(200)
        .ProducesProblem(409)
        .ProducesProblem(401)
        .RequireAuthorization("Permission:SCHOOLS_UPDATE");

        // PUT /api/admin/schools/{schoolId}/subscriptions/{id}
        group.MapPut("/{id:guid}", async (
            Guid schoolId,
            Guid id,
            [FromBody] UpdateSubscriptionRequest body,
            [FromServices] UpdateSubscriptionCommandHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var command = new UpdateSubscriptionCommand(
                    id, schoolId,
                    body.ContractStart, body.ContractEnd,
                    body.MaxConcurrentSessions, body.LoginPolicy, body.EnforceExpiry);

                var result = await handler.HandleAsync(command, ct);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("UpdateSubscription")
        .WithSummary("Update a tenant subscription for a school")
        .Produces<UpdateSubscriptionResult>()
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(401)
        .RequireAuthorization("Permission:SCHOOLS_UPDATE");

        // DELETE /api/admin/schools/{schoolId}/subscriptions/{id}
        group.MapDelete("/{id:guid}", async (
            Guid schoolId,
            Guid id,
            [FromServices] ISchoolRepository repository,
            CancellationToken ct) =>
        {
            var deleted = await repository.DeleteSubscriptionAsync(id, schoolId, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteSubscription")
        .WithSummary("Delete a tenant subscription for a school")
        .Produces(204)
        .ProducesProblem(404)
        .ProducesProblem(401)
        .RequireAuthorization("Permission:SCHOOLS_UPDATE");
    }
}

// ── Request / Response models ─────────────────────────────────────────────────

public sealed record CreateSchoolRequest(
    string Code,
    string Name,
    string? TaxId,
    string? Province,
    string? District,
    string? Address,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    DateOnly? ContractStartDate,
    DateOnly? ContractEndDate);

public sealed record UpdateSchoolRequest(
    string Name,
    string? TaxId,
    string? Province,
    string? District,
    string? Address,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    DateOnly? ContractStartDate,
    DateOnly? ContractEndDate);

public sealed record ChangeSchoolStatusRequest(string Status);

public sealed record SchoolListResult(
    IReadOnlyList<School> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record SubscriptionListResult(
    IReadOnlyList<SubscriptionDto> Items,
    int Total,
    int Page,
    int PageSize);

/// <summary>Request body for POST /subscriptions. Provide one or more TenantIds; each will produce a separate school_tenant_mapping row.</summary>
public sealed record CreateSubscriptionRequest(
    IReadOnlyList<Guid> TenantIds,
    DateOnly ContractStart,
    DateOnly ContractEnd,
    int MaxConcurrentSessions = 1,
    string LoginPolicy = "BLOCK_NEW",
    bool EnforceExpiry = true);

public sealed record UpdateSubscriptionRequest(
    DateOnly ContractStart,
    DateOnly ContractEnd,
    int MaxConcurrentSessions,
    string LoginPolicy,
    bool EnforceExpiry);
