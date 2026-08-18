using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aig.Lms.Modules.ContentManagement.Application.Content;
using Aig.Lms.Modules.ContentManagement.Application.Session;
using Dapper;
using Npgsql;

namespace Aig.Lms.Modules.ContentManagement.Infrastructure.Curriculum;

public partial class ContentManagementRepository : ILearningSessionRepository
{
    public async Task<LearningSessionDto> StartSessionAsync(StartSessionCommand command, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO user_learning_session (
                id, learning_session_id, session_id, user_id, school_id, tenant_id, content_item_id,
                start_at, last_update, inactivity_ms, device_info, ip_address, is_deleted,
                created_at, created_by, updated_at, updated_by
            )
            VALUES (
                gen_random_uuid(), @LearningSessionId, @SessionId, @UserId, @SchoolId, @TenantId, @ContentItemId,
                NOW(), NOW(), 0, @DeviceInfo::jsonb, @IpAddress::inet, FALSE,
                NOW(), @UserId, NOW(), @UserId
            )
            ON CONFLICT (learning_session_id) DO UPDATE
            SET
                session_id = EXCLUDED.session_id,
                user_id = EXCLUDED.user_id,
                school_id = EXCLUDED.school_id,
                tenant_id = EXCLUDED.tenant_id,
                content_item_id = EXCLUDED.content_item_id,
                start_at = user_learning_session.start_at,
                last_update = NOW(),
                inactivity_ms = user_learning_session.inactivity_ms + COALESCE(EXTRACT(EPOCH FROM (NOW() - user_learning_session.last_update)) * 1000, 0)::BIGINT,
                device_info = EXCLUDED.device_info,
                ip_address = EXCLUDED.ip_address,
                is_deleted = FALSE,
                updated_at = NOW(),
                updated_by = EXCLUDED.updated_by
            RETURNING id, learning_session_id AS LearningSessionId, session_id AS SessionId, start_at AS StartAt;
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var deviceInfoJson = command.DeviceInfo != null ? JsonSerializer.Serialize(command.DeviceInfo) : null;

        return await conn.QuerySingleAsync<LearningSessionDto>(new CommandDefinition(sql, new
        {
            command.LearningSessionId,
            command.SessionId,
            command.UserId,
            command.SchoolId,
            command.TenantId,
            command.ContentItemId,
            DeviceInfo = deviceInfoJson,
            command.IpAddress
        }, cancellationToken: ct));
    }

    public async Task UpdateHeartbeatAsync(HeartbeatCommand command, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE user_learning_session
            SET last_update = NOW(),
                updated_at = NOW(),
                updated_by = @UserId
            WHERE tenant_id = @TenantId
              AND user_id = @UserId
              AND learning_session_id = @LearningSessionId
              AND is_deleted = FALSE;
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            command.TenantId,
            command.UserId,
            command.LearningSessionId
        }, cancellationToken: ct));
    }

    public async Task<SessionSummaryDto> EndSessionAsync(EndSessionCommand command, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE user_learning_session
            SET last_update = NOW(),
                updated_at = NOW(),
                updated_by = @UserId
            WHERE tenant_id = @TenantId
              AND user_id = @UserId
              AND learning_session_id = @LearningSessionId
              AND is_deleted = FALSE
            RETURNING learning_session_id AS LearningSessionId, start_at AS StartAt, last_update AS LastUpdate, inactivity_ms AS InactivityMs;
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(new CommandDefinition(sql, new
        {
            command.TenantId,
            command.UserId,
            command.LearningSessionId
        }, cancellationToken: ct));

        if (row == null)
        {
            return new SessionSummaryDto(command.LearningSessionId, 0, 0, 0);
        }

        DateTime startAt = row.startat;
        DateTime lastUpdate = row.lastupdate;
        long inactivityMs = row.inactivityms;
        long totalMs = (long)(lastUpdate - startAt).TotalMilliseconds;
        long activeMs = Math.Max(0, totalMs - inactivityMs);

        return new SessionSummaryDto(command.LearningSessionId, totalMs, inactivityMs, activeMs);
    }

    public async Task<PagedResult<SessionReportDto>> GetReportAsync(SessionReportQuery query, CancellationToken ct = default)
    {
        // 1. Khởi tạo phần điều kiện lọc động (WHERE) để dùng chung cho cả 2 câu lệnh
        var filterBuilder = new System.Text.StringBuilder("WHERE tenant_id = @TenantId AND is_deleted = FALSE");
        var parameters = new DynamicParameters();
        parameters.Add("TenantId", query.TenantId);

        if (query.UserId.HasValue)
        {
            filterBuilder.Append(" AND user_id = @UserId");
            parameters.Add("UserId", query.UserId);
        }
        if (query.SchoolId.HasValue)
        {
            filterBuilder.Append(" AND school_id = @SchoolId");
            parameters.Add("SchoolId", query.SchoolId);
        }
        if (query.ContentItemId.HasValue)
        {
            filterBuilder.Append(" AND content_item_id = @ContentItemId");
            parameters.Add("ContentItemId", query.ContentItemId);
        }
        if (query.From.HasValue)
        {
            filterBuilder.Append(" AND start_at >= @From");
            parameters.Add("From", query.From);
        }
        if (query.To.HasValue)
        {
            filterBuilder.Append(" AND last_update <= @To");
            parameters.Add("To", query.To);
        }

        var filterSql = filterBuilder.ToString();

        // 2. Xây dựng câu lệnh SQL chính (Gom nhóm dựa trên INDEX trước, sau đó mới JOIN lấy tên)
        var sql = new System.Text.StringBuilder($"""
            WITH aggregated_sessions AS (
                SELECT 
                    tenant_id,
                    user_id,
                    school_id,
                    content_item_id,
                    COUNT(*) AS SessionCount,
                    SUM(EXTRACT(EPOCH FROM (last_update - start_at)) * 1000) AS TotalSessionMs,
                    COALESCE(SUM(inactivity_ms), 0) AS TotalInactivityMs,
                    SUM(EXTRACT(EPOCH FROM (last_update - start_at)) * 1000) - COALESCE(SUM(inactivity_ms), 0) AS ActiveLearningMs
                FROM user_learning_session
                {filterSql}
                GROUP BY tenant_id, user_id, school_id, content_item_id
                ORDER BY ActiveLearningMs DESC
                LIMIT @PageSize OFFSET @Offset
            )
            SELECT 
                a.tenant_id AS TenantId,        t.name AS TenantName,
                a.user_id AS UserId,            ua.fullname AS UserFullName,
                a.school_id AS SchoolId,        s.name AS SchoolName,
                a.content_item_id AS ContentItemId, ci.title AS ContentTitle,
                a.SessionCount,                 a.TotalSessionMs,
                a.TotalInactivityMs,            a.ActiveLearningMs
            FROM aggregated_sessions a
            LEFT JOIN tenant t ON a.tenant_id = t.id
            LEFT JOIN user_account ua ON a.user_id = ua.id
            LEFT JOIN school s ON a.school_id = s.id
            LEFT JOIN content_item ci ON a.content_item_id = ci.id
            ORDER BY a.ActiveLearningMs DESC;
            """);

        // 3. Xây dựng câu lệnh đếm tổng số dòng để phân trang
        var countSql = $"""
            SELECT COUNT(DISTINCT (user_id, content_item_id))
            FROM user_learning_session
            {filterSql}
            """;

        parameters.Add("PageSize", query.PageSize);
        parameters.Add("Offset", (query.Page - 1) * query.PageSize);

        // 4. Thực thi bằng Dapper
        await using var conn = new NpgsqlConnection(_connectionString);
        var items = await conn.QueryAsync<SessionReportDto>(new CommandDefinition(sql.ToString(), parameters, cancellationToken: ct));
        var totalCount = await conn.ExecuteScalarAsync<int>(new CommandDefinition(countSql, parameters, cancellationToken: ct));

        return new PagedResult<SessionReportDto>(items.AsList(), totalCount, query.Page, query.PageSize);
    }
}
