namespace Aig.Lms.Modules.Tickets.Application.Tickets.Commands;

public sealed record UpdateTicketStatusCommand(Guid TicketId, string Status, string? Note);
