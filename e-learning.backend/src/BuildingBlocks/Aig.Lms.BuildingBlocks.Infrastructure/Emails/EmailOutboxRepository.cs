using Aig.Lms.BuildingBlocks.Application.Emails;
using Aig.Lms.BuildingBlocks.Domain.Emails;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Aig.Lms.BuildingBlocks.Infrastructure.Emails;

public class EmailOutboxRepository : IEmailOutboxRepository
{
    private readonly string _connectionString;

    public EmailOutboxRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
                            ?? throw new InvalidOperationException("DefaultConnection is missing");
    }

    public async Task InsertAsync(EmailOutboxMessage message, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO email_outbox_message (id, to_email, subject, template_name, payload_json, status, created_at)
            VALUES (@Id, @ToEmail, @Subject, @TemplateName, @PayloadJson, @Status, @CreatedAt)
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            message.Id,
            message.ToEmail,
            message.Subject,
            message.TemplateName,
            message.PayloadJson,
            Status = (int)message.Status,
            message.CreatedAt
        }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<EmailOutboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id AS Id, to_email AS ToEmail, subject AS Subject, template_name AS TemplateName,
                   payload_json AS PayloadJson, status AS Status, created_at AS CreatedAt,
                   processed_at AS ProcessedAt, error AS Error
            FROM email_outbox_message
            WHERE status = 0 -- Pending
            ORDER BY created_at ASC
            LIMIT @BatchSize
            FOR UPDATE SKIP LOCKED
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var messages = await conn.QueryAsync<EmailOutboxMessage>(
            new CommandDefinition(sql, new { BatchSize = batchSize }, cancellationToken: cancellationToken));

        return messages.AsList();
    }

    public async Task UpdateStatusAsync(Guid id, EmailOutboxMessageStatus status, DateTime? processedAt, string? error, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE email_outbox_message
            SET status = @Status,
                processed_at = @ProcessedAt,
                error = @Error
            WHERE id = @Id
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = id,
            Status = (int)status,
            ProcessedAt = processedAt,
            Error = error
        }, cancellationToken: cancellationToken));
    }
}
