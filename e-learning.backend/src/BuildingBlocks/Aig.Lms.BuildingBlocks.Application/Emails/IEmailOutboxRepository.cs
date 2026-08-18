using Aig.Lms.BuildingBlocks.Domain.Emails;

namespace Aig.Lms.BuildingBlocks.Application.Emails;

public interface IEmailOutboxRepository
{
    Task InsertAsync(EmailOutboxMessage message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailOutboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid id, EmailOutboxMessageStatus status, DateTime? processedAt, string? error, CancellationToken cancellationToken = default);
}
