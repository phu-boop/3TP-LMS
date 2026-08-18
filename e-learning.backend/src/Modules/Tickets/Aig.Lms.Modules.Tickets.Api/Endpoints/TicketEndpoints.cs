using Aig.Lms.BuildingBlocks.Contracts.Tenancy;
using Aig.Lms.Modules.Tickets.Application.Tickets.Commands;
using Aig.Lms.Modules.Tickets.Application.Tickets.Commands.CreateTicket;
using Aig.Lms.Modules.Tickets.Application.Tickets.Commands.ConfirmTicketUpload;
using Aig.Lms.Modules.Tickets.Application.Tickets.Dtos;
using Aig.Lms.Modules.Tickets.Application.Tickets.Queries;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Aig.Lms.Modules.Tickets.Api.Endpoints;

public static class TicketEndpoints
{
    public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder app)
    {
        var adminGroup = app.MapGroup("/api/admin/tickets")
            .WithTags("Tickets")
            .RequireAuthorization("Permission:TICKETS_VIEW");

        adminGroup.MapGet("/", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromServices] GetTicketsQueryHandler handler,
            [FromServices] ICurrentTenant currentTenant,
            CancellationToken ct) =>
        {
            if (currentTenant.TenantId is null)
                return Results.BadRequest("Tenant context is required.");

            var result = await handler.HandleAsync(new GetTicketsQuery(currentTenant.TenantId.Value, page > 0 ? page.Value : 1, pageSize > 0 ? pageSize.Value : 10, search, status), ct);
            return Results.Ok(result);
        })
        .WithName("GetTicketsByTenant")
        .WithSummary("Get all tickets for the tenant")
        .Produces<GetTicketsResult>();

        adminGroup.MapGet("/{id}", async (
            Guid id,
            [FromServices] GetTicketByIdHandler handler,
            [FromServices] ICurrentTenant currentTenant,
            CancellationToken ct) =>
        {
            var ticket = await handler.HandleAsync(new GetTicketByIdQuery(id), ct);
            if (ticket is null || ticket.TenantId != currentTenant.TenantId)
                return Results.NotFound();

            return Results.Ok(ticket);
        })
        .WithName("GetTicketById")
        .WithSummary("Get ticket details by id")
        .Produces<TicketDto>()
        .Produces(404);

        var group = app.MapGroup("/api/tickets")
            .WithTags("Tickets")
            .RequireAuthorization();

        group.MapPatch("/{id}/status", async (
            Guid id,
            [FromBody] UpdateTicketStatusRequest request,
            [FromServices] UpdateTicketStatusCommandHandler handler,
            [FromServices] ICurrentTenant currentTenant,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            if (currentTenant.TenantId is null)
                return Results.BadRequest("Tenant context is required.");

            var updated = await handler.HandleAsync(new UpdateTicketStatusCommand(id, request.Status, request.Note), currentTenant.TenantId.Value, user, ct);
            return updated ? Results.NoContent() : Results.NotFound();
        })
        .WithName("UpdateTicketStatus")
        .WithSummary("Update ticket status")
        .Produces(204)
        .ProducesProblem(400)
        .ProducesProblem(403)
        .ProducesProblem(404);

        // GET /api/tickets/my-tickets
        group.MapGet("/my-tickets", async (
            ClaimsPrincipal user,
            [FromServices] ICurrentTenant currentTenant,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? status,
            [FromQuery] string? search,
            [FromServices] GetMyTicketsHandler handler,
            CancellationToken ct) =>
        {
            var actorId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(actorId, out var actorGuid))
                return Results.Unauthorized();

            if (currentTenant.TenantId is null)
                return Results.BadRequest("Tenant context is required.");

            var result = await handler.HandleAsync(new GetMyTicketsQuery(actorGuid, currentTenant.TenantId.Value, page ?? 1, pageSize ?? 20, status, search), ct);
            return Results.Ok(result);
        })
        .WithName("GetMyTickets")
        .WithSummary("Get a paginated list of tickets created by the current user")
        .Produces<GetMyTicketsResult>()
        .ProducesProblem(401);

        // GET /api/tickets/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            [FromServices] ICurrentTenant currentTenant,
            [FromServices] GetTicketDetailHandler handler,
            CancellationToken ct) =>
        {
            var actorId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(actorId, out var actorGuid))
                return Results.Unauthorized();

            if (currentTenant.TenantId is null)
                return Results.BadRequest("Tenant context is required.");

            // Lấy chi tiết ticket và truyền vào ID của user và tenant hiện tại để xác minh quyền xem
            var ticket = await handler.HandleAsync(new GetTicketDetailQuery(id, actorGuid, currentTenant.TenantId.Value), ct);
            
            return ticket is null ? Results.NotFound() : Results.Ok(ticket);
        })
        .WithName("GetTicketDetail")
        .WithSummary("Get ticket details by ID (Only allowed if created by current user)")
        .ProducesProblem(404)
        .ProducesProblem(401);

        // POST /api/tickets
        group.MapPost("/", async (
            [FromBody] CreateTicketRequest request,
            ClaimsPrincipal user,
            [FromServices] ICurrentTenant currentTenant,
            [FromServices] CreateTicketCommandHandler handler,
            CancellationToken ct) =>
        {
            var actorId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(actorId, out var actorGuid))
                return Results.Unauthorized();

            if (currentTenant.TenantId is null)
                return Results.BadRequest("Tenant context is required.");

            var schoolIdStr = user.FindFirstValue("school_id");
            Guid schoolId = Guid.TryParse(schoolIdStr, out var s) ? s : Guid.Empty;

            var result = await handler.HandleAsync(new CreateTicketCommand(
                currentTenant.TenantId.Value,
                schoolId,
                actorGuid,
                request.Title,
                request.Description,
                request.FileNames), ct);

            return Results.Ok(result);
        })
        .WithName("CreateTicket")
        .WithSummary("Create a new ticket and request upload URLs for attachments")
        .Produces<CreateTicketResult>(200)
        .ProducesProblem(400)
        .ProducesProblem(401);

        // POST /api/tickets/{id}/confirm-upload
        group.MapPost("/{id:guid}/confirm-upload", async (
            Guid id,
            ClaimsPrincipal user,
            [FromServices] ICurrentTenant currentTenant,
            [FromServices] ConfirmTicketUploadCommandHandler handler,
            CancellationToken ct) =>
        {
            var actorId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(actorId, out var actorGuid))
                return Results.Unauthorized();

            if (currentTenant.TenantId is null)
                return Results.BadRequest("Tenant context is required.");

            try
            {
                await handler.HandleAsync(new ConfirmTicketUploadCommand(
                    currentTenant.TenantId.Value,
                    id,
                    actorGuid), ct);

                return Results.Ok(new { message = "Upload confirmed successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("ConfirmTicketUpload")
        .WithSummary("Confirm attachment uploads for a ticket")
        .Produces(200)
        .ProducesProblem(400)
        .ProducesProblem(401);

        return app;
    }

    private static Guid? ExtractActorId(HttpContext ctx)
    {
        var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var g) ? g : null;
    }

    private static Guid? ExtractTenantId(HttpContext ctx)
    {
        var tenantStr = ctx.User.FindFirstValue("tenant_id");
        return Guid.TryParse(tenantStr, out var g) ? g : null;
    }
}

public sealed record CreateTicketRequest(
    string Title,
    string Description,
    System.Collections.Generic.List<string>? FileNames
);
