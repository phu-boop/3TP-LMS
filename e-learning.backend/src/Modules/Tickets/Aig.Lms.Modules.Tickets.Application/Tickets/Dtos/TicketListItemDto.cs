using Aig.Lms.Modules.Tickets.Application.Tickets.Dtos;

namespace Aig.Lms.Modules.Tickets.Application.Tickets.Dtos;

public sealed record TicketListItemDto(
    Guid Id,
    string Title,
    string Description,
    string Status,
    string? ResolutionNote,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public TicketUserDto Reporter { get; init; } = default!;
}