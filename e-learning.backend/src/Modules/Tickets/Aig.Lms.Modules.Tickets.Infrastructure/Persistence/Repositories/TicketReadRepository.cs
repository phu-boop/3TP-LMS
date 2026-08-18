using Aig.Lms.Modules.Tickets.Application.Abstractions;
using Aig.Lms.Modules.Tickets.Application.Tickets.Dtos;
using Aig.Lms.Modules.Tickets.Infrastructure.Persistence;
using Dapper;

namespace Aig.Lms.Modules.Tickets.Infrastructure.Persistence.Repositories;

public sealed class TicketReadRepository : ITicketReadRepository
{
    private readonly TicketsDbContext _context;

    public TicketReadRepository(TicketsDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<MyTicketDto>> ListByCreatorAsync(
        Guid creatorId, 
        Guid tenantId,
        int page, 
        int pageSize, 
        string? status, 
        string? search, 
        CancellationToken ct = default)
    {
        var sql = """
            SELECT id AS Id, 
                   tenant_id AS TenantId, 
                   creator_id AS CreatorId, 
                   title AS Title, 
                   status AS Status, 
                   created_at AS CreatedAt, 
                   updated_at AS UpdatedAt
            FROM ticket
            WHERE creator_id = @CreatorId AND tenant_id = @TenantId
            """;

        if (!string.IsNullOrWhiteSpace(status))
            sql += " AND status = @Status";

        if (!string.IsNullOrWhiteSpace(search))
            sql += " AND title ILIKE @Search";

        sql += " ORDER BY created_at DESC LIMIT @PageSize OFFSET @Offset";

        await using var conn = _context.CreateConnection();
        var results = await conn.QueryAsync<MyTicketDto>(new CommandDefinition(sql, new
        {
            CreatorId = creatorId,
            TenantId = tenantId,
            Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
            Search = $"%{search?.Trim()}%",
            PageSize = pageSize,
            Offset = (page - 1) * pageSize,
        }, cancellationToken: ct));

        return results.AsList();
    }

    public async Task<int> CountByCreatorAsync(Guid creatorId, Guid tenantId, string? status, string? search, CancellationToken ct = default)
    {
        var sql = "SELECT COUNT(1) FROM ticket WHERE creator_id = @CreatorId AND tenant_id = @TenantId";

        if (!string.IsNullOrWhiteSpace(status))
            sql += " AND status = @Status";

        if (!string.IsNullOrWhiteSpace(search))
            sql += " AND title ILIKE @Search";

        await using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new
        {
            CreatorId = creatorId,
            TenantId = tenantId,
            Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
            Search = $"%{search?.Trim()}%",
        }, cancellationToken: ct));
    }

    public async Task<TicketDetailDto?> GetDetailAsync(Guid ticketId, Guid? creatorId = null, Guid? tenantId = null, CancellationToken ct = default)
    {
        var sql = """
            SELECT id AS Id, 
                   tenant_id AS TenantId, 
                   creator_id AS CreatorId, 
                   title AS Title, 
                   description AS Description,
                   status AS Status, 
                   resolution_note AS ResolutionNote,
                   created_at AS CreatedAt, 
                   updated_at AS UpdatedAt
            FROM ticket
            WHERE id = @TicketId
            """;

        if (creatorId.HasValue)
            sql += " AND creator_id = @CreatorId";
            
        if (tenantId.HasValue)
            sql += " AND tenant_id = @TenantId";

        await using var conn = _context.CreateConnection();
        
        var ticket = await conn.QueryFirstOrDefaultAsync<TicketDetailDto>(
            new CommandDefinition(sql, new { TicketId = ticketId, CreatorId = creatorId, TenantId = tenantId }, cancellationToken: ct));

        if (ticket == null)
            return null;

        var attachmentsSql = """
            SELECT id AS Id,
                   file_name AS FileName,
                   file_path AS FilePath,
                   content_type AS ContentType,
                   file_size AS FileSize,
                   created_at AS CreatedAt
            FROM ticket_attachment
            WHERE ticket_id = @TicketId
            """;
            
        var attachments = await conn.QueryAsync<TicketAttachmentDto>(
            new CommandDefinition(attachmentsSql, new { TicketId = ticketId }, cancellationToken: ct));

        // Use reflection or constructor to set attachments if it's a record with init-only properties.
        // Since TicketDetailDto is a record with primary constructor, we reconstruct it.
        return ticket with { Attachments = attachments.AsList() };
    }

    public async Task<TicketDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT t.id, t.tenant_id AS TenantId, t.school_id AS SchoolId,
                   t.title, t.description, t.status, t.resolution_note AS ResolutionNote,
                   t.created_at AS CreatedAt, t.updated_at AS UpdatedAt,
                   u.id AS Id, u.full_name AS Name, u.avatar_url AS AvatarUrl
            FROM ticket t
            LEFT JOIN user_account u ON t.creator_id = u.id
            WHERE t.id = @Id
            LIMIT 1
            """;

        await using var conn = _context.CreateConnection();
        var result = await conn.QueryAsync<TicketDto, TicketUserDto, TicketDto>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct),
            (ticket, reporter) => ticket with { Reporter = reporter ?? new TicketUserDto(Guid.Empty, "Unknown", null) },
            splitOn: "Id");

        var ticket = result.FirstOrDefault();
        if (ticket is null) return null;

        const string attachmentsSql = """
            SELECT file_path
            FROM ticket_attachment
            WHERE ticket_id = @TicketId
            """;

        var attachmentUrls = await conn.QueryAsync<string>(
            new CommandDefinition(attachmentsSql, new { TicketId = id }, cancellationToken: ct));

        return ticket with { Attachments = attachmentUrls.AsList() };
    }

    public async Task<IReadOnlyList<TicketListItemDto>> ListByTenantIdAsync(
        Guid tenantId,
        int page,
        int pageSize,
        string? search,
        string? status,
        CancellationToken ct = default)
    {
        var searchCondition = string.IsNullOrWhiteSpace(search) ? "" : "AND (t.title ILIKE @Search OR t.description ILIKE @Search)";
        var statusCondition = string.IsNullOrWhiteSpace(status) ? "" : "AND t.status = @Status";
        
        var sql = $"""
            SELECT t.id, t.title, t.description, t.status, t.resolution_note AS ResolutionNote, t.created_at AS CreatedAt, t.updated_at AS UpdatedAt,
                   u.id AS Id, u.full_name AS Name, u.avatar_url AS AvatarUrl
            FROM ticket t
            LEFT JOIN user_account u ON t.creator_id = u.id
            WHERE t.tenant_id = @TenantId {searchCondition} {statusCondition}
            ORDER BY t.created_at DESC
            LIMIT @Limit OFFSET @Offset
            """;

        await using var conn = _context.CreateConnection();
        
        var parameters = new 
        { 
            TenantId = tenantId, 
            Search = $"%{search}%",
            Status = status,
            Limit = pageSize,
            Offset = (page - 1) * pageSize
        };

        var rows = await conn.QueryAsync<TicketListItemDto, TicketUserDto, TicketListItemDto>(
            new CommandDefinition(sql, parameters, cancellationToken: ct),
            (ticket, reporter) => ticket with { Reporter = reporter ?? new TicketUserDto(Guid.Empty, "Unknown", null) },
            splitOn: "Id");

        return rows.ToList().AsReadOnly();
    }

    public async Task<int> CountByTenantIdAsync(Guid tenantId, string? search, string? status, CancellationToken ct = default)
    {
        var searchCondition = string.IsNullOrWhiteSpace(search) ? "" : "AND (title ILIKE @Search OR description ILIKE @Search)";
        var statusCondition = string.IsNullOrWhiteSpace(status) ? "" : "AND status = @Status";
        
        var countSql = $"""
            SELECT COUNT(*)
            FROM ticket
            WHERE tenant_id = @TenantId {searchCondition} {statusCondition}
            """;

        await using var conn = _context.CreateConnection();
        
        var parameters = new 
        { 
            TenantId = tenantId, 
            Search = $"%{search}%",
            Status = status
        };

        return await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: ct));
    }
}
