using Aig.Lms.Modules.Tickets.Application.Tickets.Dtos;

namespace Aig.Lms.Modules.Tickets.Application.Tickets.Dtos;

public sealed record TicketDto(
    Guid Id,
    Guid TenantId,
    Guid SchoolId,
    string Title,
    string Description,
    string Status,
    string? ResolutionNote,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public TicketUserDto Reporter { get; init; } = default!;
    public List<string> Attachments { get; init; } = new();
}
