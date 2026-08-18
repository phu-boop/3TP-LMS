using Aig.Lms.Modules.Tickets.Domain.Entities;
using Aig.Lms.Modules.Tickets.Domain.Repositories;
using Dapper;

namespace Aig.Lms.Modules.Tickets.Infrastructure.Persistence.Repositories;

public sealed class TicketRepository : ITicketRepository
{
    private readonly TicketsDbContext _context;

    public TicketRepository(TicketsDbContext context)
    {
        _context = context;
    }

    public async Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string ticketSql = """
            SELECT id, tenant_id AS TenantId, school_id AS SchoolId, creator_id AS CreatorId,
                   title, description, status, resolution_note AS ResolutionNote,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM ticket
            WHERE id = @Id
            LIMIT 1
            """;

        const string attachmentsSql = """
            SELECT id, ticket_id AS TicketId, file_name AS FileName, file_path AS FilePath,
                   content_type AS ContentType, file_size AS FileSize, created_at AS CreatedAt
            FROM ticket_attachment
            WHERE ticket_id = @TicketId
            """;

        await using var conn = _context.CreateConnection();
        var ticketRow = await conn.QueryFirstOrDefaultAsync<TicketRow>(
            new CommandDefinition(ticketSql, new { Id = id }, cancellationToken: ct));

        if (ticketRow is null)
            return null;

        var attachmentRows = await conn.QueryAsync<AttachmentRow>(
            new CommandDefinition(attachmentsSql, new { TicketId = id }, cancellationToken: ct));

        return ticketRow.ToDomain(attachmentRows?.Select(r => r.ToDomain()).ToList());
    }

    public async Task AddAsync(Ticket ticket, CancellationToken ct = default)
    {
        const string ticketSql = """
            INSERT INTO ticket (id, tenant_id, school_id, creator_id, title, description, status, resolution_note, created_at, updated_at)
            VALUES (@Id, @TenantId, @SchoolId, @CreatorId, @Title, @Description, @Status, @ResolutionNote, @CreatedAt, @UpdatedAt)
            """;

        const string attachmentSql = """
            INSERT INTO ticket_attachment (id, ticket_id, file_name, file_path, content_type, file_size, created_at)
            VALUES (@Id, @TicketId, @FileName, @FilePath, @ContentType, @FileSize, @CreatedAt)
            """;

        await using var conn = _context.CreateConnection();
        await conn.OpenAsync(ct);
        await using var transaction = await conn.BeginTransactionAsync(ct);

        try
        {
            await conn.ExecuteAsync(new CommandDefinition(ticketSql, new
            {
                ticket.Id,
                ticket.TenantId,
                ticket.SchoolId,
                ticket.CreatorId,
                ticket.Title,
                ticket.Description,
                ticket.Status,
                ticket.ResolutionNote,
                ticket.CreatedAt,
                ticket.UpdatedAt
            }, transaction, cancellationToken: ct));

            foreach (var attachment in ticket.Attachments)
            {
                await conn.ExecuteAsync(new CommandDefinition(attachmentSql, new
                {
                    attachment.Id,
                    attachment.TicketId,
                    attachment.FileName,
                    attachment.FilePath,
                    attachment.ContentType,
                    attachment.FileSize,
                    attachment.CreatedAt
                }, transaction, cancellationToken: ct));
            }

            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task UpdateAsync(Ticket ticket, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE ticket
            SET status = @Status,
                resolution_note = @ResolutionNote,
                updated_at = @UpdatedAt
            WHERE id = @Id
            """;

        await using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            ticket.Status,
            ticket.ResolutionNote,
            ticket.UpdatedAt,
            ticket.Id
        }, cancellationToken: ct));
    }

    public async Task AddAttachmentAsync(TicketAttachment attachment, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO ticket_attachment (id, ticket_id, file_name, file_path, content_type, file_size, created_at)
            VALUES (@Id, @TicketId, @FileName, @FilePath, @ContentType, @FileSize, @CreatedAt)
            """;

        await using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            attachment.Id,
            attachment.TicketId,
            attachment.FileName,
            attachment.FilePath,
            attachment.ContentType,
            attachment.FileSize,
            attachment.CreatedAt
        }, cancellationToken: ct));
    }

    public async Task UpdateAttachmentAsync(TicketAttachment attachment, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE ticket_attachment
            SET content_type = @ContentType,
                file_size = @FileSize
            WHERE id = @Id
            """;

        await using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            attachment.ContentType,
            attachment.FileSize,
            attachment.Id
        }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Ticket>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, tenant_id AS TenantId, school_id AS SchoolId, creator_id AS CreatorId,
                   title, description, status, resolution_note AS ResolutionNote,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM ticket
            WHERE tenant_id = @TenantId
            ORDER BY created_at DESC
            """;

        await using var conn = _context.CreateConnection();
        var rows = await conn.QueryAsync<TicketRow>(
            new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: ct));

        return rows.Select(r => r.ToDomain()).ToList().AsReadOnly();
    }

    public async Task<(IReadOnlyList<Ticket> Items, int Total)> GetByTenantIdPagedAsync(Guid tenantId, int page, int pageSize, string? search = null, CancellationToken ct = default)
    {
        var searchCondition = string.IsNullOrWhiteSpace(search) ? "" : "AND (title ILIKE @Search OR description ILIKE @Search)";
        
        var countSql = $"""
            SELECT COUNT(*)
            FROM ticket
            WHERE tenant_id = @TenantId {searchCondition}
            """;

        var sql = $"""
            SELECT id, tenant_id AS TenantId, school_id AS SchoolId, creator_id AS CreatorId,
                   title, description, status, resolution_note AS ResolutionNote,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM ticket
            WHERE tenant_id = @TenantId {searchCondition}
            ORDER BY created_at DESC
            LIMIT @Limit OFFSET @Offset
            """;

        await using var conn = _context.CreateConnection();
        
        var parameters = new 
        { 
            TenantId = tenantId, 
            Search = $"%{search}%",
            Limit = pageSize,
            Offset = (page - 1) * pageSize
        };

        var total = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: ct));

        var rows = await conn.QueryAsync<TicketRow>(
            new CommandDefinition(sql, parameters, cancellationToken: ct));

        return (rows.Select(r => r.ToDomain()).ToList().AsReadOnly(), total);
    }

    private sealed class TicketRow
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid SchoolId { get; set; }
        public Guid CreatorId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ResolutionNote { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Ticket ToDomain(List<TicketAttachment>? attachments = null) => Ticket.Reconstitute(
            Id,
            TenantId,
            SchoolId,
            CreatorId,
            Title,
            Description,
            Status,
            ResolutionNote,
            CreatedAt,
            UpdatedAt,
            attachments);
    }

    private sealed class AttachmentRow
    {
        public Guid Id { get; set; }
        public Guid TicketId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime CreatedAt { get; set; }

        public TicketAttachment ToDomain() => TicketAttachment.Reconstitute(
            Id,
            TicketId,
            FileName,
            FilePath,
            ContentType,
            FileSize,
            CreatedAt);
    }
}
