using Aig.Lms.BuildingBlocks.Domain.Entities;

namespace Aig.Lms.BuildingBlocks.Domain.Emails;

public class EmailOutboxMessage : Entity<Guid>
{
    public string ToEmail { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string TemplateName { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public EmailOutboxMessageStatus Status { get; private set; } = EmailOutboxMessageStatus.Pending;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; private set; }
    public string? Error { get; private set; }

    protected EmailOutboxMessage() { } // For EF Core

    public EmailOutboxMessage(string toEmail, string subject, string templateName, string payloadJson)
    {
        Id = Guid.NewGuid();
        ToEmail = toEmail;
        Subject = subject;
        TemplateName = templateName;
        PayloadJson = payloadJson;
        Status = EmailOutboxMessageStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsProcessing()
    {
        Status = EmailOutboxMessageStatus.Processing;
    }

    public void MarkAsSent(DateTime processedAt)
    {
        Status = EmailOutboxMessageStatus.Sent;
        ProcessedAt = processedAt;
    }

    public void MarkAsFailed(DateTime processedAt, string error)
    {
        Status = EmailOutboxMessageStatus.Failed;
        ProcessedAt = processedAt;
        Error = error;
    }
}
