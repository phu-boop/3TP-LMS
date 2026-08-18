using Aig.Lms.Modules.Tickets.Domain.Entities;

namespace Aig.Lms.Modules.Tickets.Application.Tickets.Dtos;

public sealed record UpdateTicketStatusRequest(string Status, string? Note);
