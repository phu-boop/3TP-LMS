namespace Aig.Lms.Modules.Tickets.Application.Tickets.Dtos;

public sealed record TicketAttachmentDto(
    Guid Id,
    string FileName,
    string FilePath,
    string ContentType,
    long FileSize,
    DateTime CreatedAt);
