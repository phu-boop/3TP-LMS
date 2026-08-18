namespace Aig.Lms.Modules.Tickets.Application.Tickets.Dtos;

public sealed record TicketDetailDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid CreatorId { get; init; }
    public string Title { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string? ResolutionNote { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public IReadOnlyList<TicketAttachmentDto> Attachments { get; init; } = Array.Empty<TicketAttachmentDto>();
}
