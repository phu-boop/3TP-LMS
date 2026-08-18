using Aig.Lms.BuildingBlocks.Domain.Entities;

namespace Aig.Lms.Modules.Tickets.Domain.Entities;

public sealed class TicketAttachment : Entity<Guid>
{
    public Guid TicketId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public DateTime CreatedAt { get; private init; }

    private TicketAttachment() { }

    private TicketAttachment(
        Guid id,
        Guid ticketId,
        string fileName,
        string filePath,
        string contentType,
        long fileSize,
        DateTime createdAt)
    {
        Id = id;
        TicketId = ticketId;
        FileName = fileName;
        FilePath = filePath;
        ContentType = contentType;
        FileSize = fileSize;
        CreatedAt = createdAt;
    }

    public static TicketAttachment Create(
        Guid ticketId,
        string fileName,
        string filePath,
        string contentType,
        long fileSize) =>
        new(
            Guid.NewGuid(),
            ticketId,
            fileName,
            filePath,
            contentType,
            fileSize,
            DateTime.UtcNow);

    public static TicketAttachment Reconstitute(
        Guid id,
        Guid ticketId,
        string fileName,
        string filePath,
        string contentType,
        long fileSize,
        DateTime createdAt) =>
        new(
            id,
            ticketId,
            fileName,
            filePath,
            contentType,
            fileSize,
            createdAt);

    public void ConfirmUpload(string contentType, long fileSize)
    {
        ContentType = contentType;
        FileSize = fileSize;
    }
}
