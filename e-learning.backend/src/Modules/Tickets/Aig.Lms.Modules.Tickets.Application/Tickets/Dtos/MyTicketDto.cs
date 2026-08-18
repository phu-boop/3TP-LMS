namespace Aig.Lms.Modules.Tickets.Application.Tickets.Dtos;

public sealed record MyTicketDto(
    Guid Id,
    Guid TenantId,
    Guid CreatorId,
    string Title,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
