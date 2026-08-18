using System.Security.Claims;
using Aig.Lms.BuildingBlocks.Application.Emails;
using Aig.Lms.BuildingBlocks.Domain.Exceptions;
using Aig.Lms.Modules.Tickets.Domain.Entities;
using Aig.Lms.Modules.Tickets.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Aig.Lms.Modules.Identity.Application.Auth;

namespace Aig.Lms.Modules.Tickets.Application.Tickets.Commands;

public sealed class UpdateTicketStatusCommandHandler
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IEmailOutboxService _emailOutboxService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UpdateTicketStatusCommandHandler> _logger;

    public UpdateTicketStatusCommandHandler(
        ITicketRepository ticketRepository,
        IEmailOutboxService emailOutboxService,
        IUserRepository userRepository,
        ILogger<UpdateTicketStatusCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _emailOutboxService = emailOutboxService;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(UpdateTicketStatusCommand request, Guid tenantId, ClaimsPrincipal user, CancellationToken ct = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, ct);
        if (ticket is null || ticket.TenantId != tenantId)
            return false;

        var isAdmin = IsTenantAdmin(user);
        var isSchoolClient = IsSchoolClient(user);

        var normalizedStatus = TicketStatus.Normalize(request.Status);

        if (isAdmin)
        {
            if (normalizedStatus == TicketStatus.InProgress)
            {
                ticket.MarkInProgress();
            }
            else if (normalizedStatus == TicketStatus.Resolved)
            {
                if (string.IsNullOrWhiteSpace(request.Note))
                    throw new DomainException("Resolution note is required for resolving a ticket.");
                ticket.Resolve(request.Note);

                // Enqueue email to customer
                var creatorUser = await _userRepository.FindByIdAsync(ticket.CreatorId, ct);
                if (creatorUser is not null && !string.IsNullOrWhiteSpace(creatorUser.Email))
                {
                    var subject = $"[LMS Support] Ticket Resolved: {ticket.Title}";
                    var payload = new
                    {
                        UserName = creatorUser.FullName,
                        TicketCode = ticket.Id.ToString(),
                        Title = ticket.Title,
                        ResolutionNote = request.Note
                    };
                    try
                    {
                        await _emailOutboxService.EnqueueAsync(creatorUser.Email, subject, "TicketResolved", payload, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to enqueue resolution email to {Email} for ticket {TicketId}", creatorUser.Email, ticket.Id);
                    }
                }
            }
            else
            {
                throw new DomainException("Admin can only set status to InProgress or Resolved.");
            }
        }
        else if (isSchoolClient)
        {
            var userId = GetUserId(user);
            if (!userId.HasValue || ticket.CreatorId != userId.Value)
                throw new DomainException("School Client can only close their own tickets.");

            if (normalizedStatus == TicketStatus.Closed)
            {
                if (string.IsNullOrWhiteSpace(request.Note))
                    throw new DomainException("Note is required for closing a ticket.");
                ticket.Close(request.Note);
            }
            else
            {
                throw new DomainException("School Client can only close tickets.");
            }
        }
        else
        {
            throw new DomainException("You are not authorized to update this ticket's status.");
        }

        await _ticketRepository.UpdateAsync(ticket, ct);
        return true;
    }

    private static bool IsTenantAdmin(ClaimsPrincipal user)
    {
        return user.FindAll(ClaimTypes.Role)
                   .Any(c => string.Equals(c.Value, "TENANT_ADMIN", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSchoolClient(ClaimsPrincipal user)
    {
        return user.FindAll(ClaimTypes.Role)
                   .Any(c => string.Equals(c.Value, "SCHOOL", StringComparison.OrdinalIgnoreCase));
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var userId) ? userId : null;
    }
}