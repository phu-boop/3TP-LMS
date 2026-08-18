using System.Text;
using Aig.Lms.Modules.Schools.Application;
using Aig.Lms.Modules.Schools.Domain;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Aig.Lms.Modules.Schools.Infrastructure;

public sealed class SchoolRepository : ISchoolRepository
{
    private readonly string _connectionString;

    public SchoolRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
    }

    // ── Schools ───────────────────────────────────────────────────────────────

    public async Task<bool> CodeExistsAsync(string code, CancellationToken ct = default)
    {
        const string sql = "SELECT COUNT(1) FROM school WHERE code = @Code";
        await using var conn = new NpgsqlConnection(_connectionString);
        var count = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { Code = code.ToUpperInvariant() }, cancellationToken: ct));
        return count > 0;
    }

    public async Task CreateAsync(School school, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO school (
                id, code, name, tax_id,
                province_code, district_code, address,
                contact_name, contact_email, contact_phone,
                contract_start_date, contract_end_date,
                status, created_at, updated_at)
            VALUES (
                @Id, @Code, @Name, @TaxId,
                @Province, @District, @Address,
                @ContactName, @ContactEmail, @ContactPhone,
                @ContractStartDate, @ContractEndDate,
                @Status::common_status, NOW(), NOW())
            """;
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            school.Id, school.Code, school.Name, school.TaxId,
            school.Province, school.District, school.Address,
            school.ContactName, school.ContactEmail, school.ContactPhone,
            school.ContractStartDate, school.ContractEndDate,
            school.Status
        }, cancellationToken: ct));
    }

    public async Task<School?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, code, name, tax_id AS TaxId,
                   province_code AS Province, district_code AS District, address,
                   contact_name AS ContactName, contact_email AS ContactEmail, contact_phone AS ContactPhone,
                   contract_start_date AS ContractStartDate, contract_end_date AS ContractEndDate,
                   status::TEXT AS Status, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM school
            WHERE id = @Id AND status != 'DELETED'::common_status
            LIMIT 1
            """;
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<School>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    private static (string whereClause, DynamicParameters parameters) BuildSchoolFilter(
        string? status,
        string? search,
        string? province,
        string? district,
        Guid? tenantId = null)
    {
        var join = new StringBuilder();
        var where = new StringBuilder("WHERE s.status != 'DELETED'::common_status");
        var parameters = new DynamicParameters();

        if (tenantId.HasValue)
        {
            join.Append(" INNER JOIN school_tenant_mapping m ON m.school_id = s.id AND m.tenant_id = @TenantId AND m.is_deleted = FALSE");
            parameters.Add("TenantId", tenantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            where.Append(" AND s.status = @Status::common_status");
            parameters.Add("Status", status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Append(" AND (s.name ILIKE @Search OR s.code ILIKE @Search)");
            parameters.Add("Search", $"%{search}%");
        }

        if (!string.IsNullOrWhiteSpace(province))
        {
            where.Append(" AND s.province_code = @Province");
            parameters.Add("Province", province);
        }

        if (!string.IsNullOrWhiteSpace(district))
        {
            where.Append(" AND s.district_code = @District");
            parameters.Add("District", district);
        }

        return ($"{join} {where}", parameters);
    }

    // ── List with pagination (main list screen) ───────────────────────────────

    public async Task<IReadOnlyList<School>> ListAsync(
        int page, int pageSize, string? status, string? search, string? province, string? district,
        Guid? tenantId, CancellationToken ct = default)
    {
        var (filterClause, parameters) = BuildSchoolFilter(status, search, province, district, tenantId);

        var sql = $"""
            SELECT s.id, s.code, s.name, s.tax_id AS TaxId,
                   s.province_code AS Province, s.district_code AS District, s.address,
                   s.contact_name AS ContactName, s.contact_email AS ContactEmail, s.contact_phone AS ContactPhone,
                   s.contract_start_date AS ContractStartDate, s.contract_end_date AS ContractEndDate,
                   s.status::TEXT AS Status, s.created_at AS CreatedAt, s.updated_at AS UpdatedAt
            FROM school s
            {filterClause}
            ORDER BY s.name ASC
            LIMIT @PageSize OFFSET @Offset
            """;

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        await using var conn = new NpgsqlConnection(_connectionString);
        var results = await conn.QueryAsync<School>(new CommandDefinition(sql, parameters, cancellationToken: ct));
        return results.AsList();
    }

    public async Task<int> CountAsync(
        string? status, string? search, string? province, string? district,
        Guid? tenantId, CancellationToken ct = default)
    {
        var (filterClause, parameters) = BuildSchoolFilter(status, search, province, district, tenantId);

        var sql = $"SELECT COUNT(1) FROM school s {filterClause}";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, parameters, cancellationToken: ct));
    }

    // ── List all without pagination (export) ──────────────────────────────────
    //
    // NOTE: ListAllAsync intentionally ignores tenantId because SchoolManagement.tsx
    // (the only caller of this export feature) always loads ALL schools without
    // tenant scoping (filterByCurrentTenant is never sent from that screen).
    // If the UI is updated to use filterByCurrentTenant=true in the future,
    // this method MUST be updated to accept and apply tenantId to avoid data leakage.

    public async Task<IReadOnlyList<School>> ListAllAsync(
        string? status, string? search, string? province, string? district,
        CancellationToken ct = default)
    {
        var (filterClause, parameters) = BuildSchoolFilter(status, search, province, district, tenantId: null);

        // LIMIT 10001 so the endpoint can detect > 10000 and return an error.
        var sql = $"""
            SELECT s.id, s.code, s.name, s.tax_id AS TaxId,
                   s.province_code AS Province, s.district_code AS District, s.address,
                   s.contact_name AS ContactName, s.contact_email AS ContactEmail, s.contact_phone AS ContactPhone,
                   s.contract_start_date AS ContractStartDate, s.contract_end_date AS ContractEndDate,
                   s.status::TEXT AS Status, s.created_at AS CreatedAt, s.updated_at AS UpdatedAt
            FROM school s
            {filterClause}
            ORDER BY s.name ASC
            LIMIT 10001
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var results = await conn.QueryAsync<School>(new CommandDefinition(sql, parameters, cancellationToken: ct));
        return results.AsList();
    }

    public async Task<UpdateSchoolResult?> UpdateAsync(UpdateSchoolCommand command, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE school
            SET name                = @Name,
                tax_id              = @TaxId,
                province_code       = @Province,
                district_code       = @District,
                address             = @Address,
                contact_name        = @ContactName,
                contact_email       = @ContactEmail,
                contact_phone       = @ContactPhone,
                contract_start_date = @ContractStartDate,
                contract_end_date   = @ContractEndDate,
                updated_at          = NOW()
            WHERE id = @SchoolId AND status != 'DELETED'::common_status
            RETURNING id AS SchoolId, code, name,
                      tax_id AS TaxId,
                      province_code AS Province, district_code AS District, address,
                      contact_name AS ContactName, contact_email AS ContactEmail, contact_phone AS ContactPhone,
                      contract_start_date AS ContractStartDate, contract_end_date AS ContractEndDate,
                      status::TEXT AS Status
            """;
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<UpdateSchoolResult>(
            new CommandDefinition(sql, new
            {
                command.SchoolId, command.Name, command.TaxId,
                command.Province, command.District, command.Address,
                command.ContactName, command.ContactEmail, command.ContactPhone,
                command.ContractStartDate, command.ContractEndDate
            }, cancellationToken: ct));
    }

    public async Task<bool> ChangeStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE school
            SET status = @Status::common_status, updated_at = NOW()
            WHERE id = @Id AND status != 'DELETED'::common_status
            """;
        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id, Status = status }, cancellationToken: ct));
        return rows > 0;
    }

    public async Task<bool> SchoolExistsAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "SELECT COUNT(1) FROM school WHERE id = @Id AND status != 'DELETED'::common_status";
        await using var conn = new NpgsqlConnection(_connectionString);
        var count = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
        return count > 0;
    }

    // ── Subscriptions (school_tenant_mapping) ─────────────────────────────────

    public async Task<IReadOnlyList<SubscriptionDto>> ListSubscriptionsAsync(
        Guid schoolId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT m.id             AS Id,
                   m.school_id      AS SchoolId,
                   m.tenant_id      AS TenantId,
                   t.code           AS TenantCode,
                   t.name           AS TenantName,
                   m.contract_start AS ContractStart,
                   m.contract_end   AS ContractEnd,
                   m.max_concurrent_sessions AS MaxConcurrentSessions,
                   m.login_policy::TEXT       AS LoginPolicy,
                   m.enforce_expiry           AS EnforceExpiry,
                   m.status::TEXT             AS Status,
                   m.created_at               AS CreatedAt,
                   m.updated_at               AS UpdatedAt
            FROM school_tenant_mapping m
            JOIN tenant t ON t.id = m.tenant_id
            WHERE m.school_id = @SchoolId
              AND m.is_deleted = FALSE
            ORDER BY m.created_at DESC
            """;
        await using var conn = new NpgsqlConnection(_connectionString);
        var results = await conn.QueryAsync<SubscriptionDto>(
            new CommandDefinition(sql, new { SchoolId = schoolId }, cancellationToken: ct));
        return results.AsList();
    }

    public async Task<IReadOnlyList<SubscriptionDto>> ListSubscriptionsAsync(
        Guid? schoolId,
        Guid? tenantId,
        string? status,
        IReadOnlyList<Guid>? allowedTenantIds,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var sql = """
            SELECT m.id             AS Id,
                   m.school_id      AS SchoolId,
                   m.tenant_id      AS TenantId,
                   t.code           AS TenantCode,
                   t.name           AS TenantName,
                   m.contract_start AS ContractStart,
                   m.contract_end   AS ContractEnd,
                   m.max_concurrent_sessions AS MaxConcurrentSessions,
                   m.login_policy::TEXT       AS LoginPolicy,
                   m.enforce_expiry           AS EnforceExpiry,
                   m.status::TEXT             AS Status,
                   m.created_at               AS CreatedAt,
                   m.updated_at               AS UpdatedAt
            FROM school_tenant_mapping m
            JOIN tenant t ON t.id = m.tenant_id
            WHERE m.is_deleted = FALSE
              AND (@SchoolId IS NULL OR m.school_id = @SchoolId)
              AND (@TenantId IS NULL OR m.tenant_id = @TenantId)
              AND (@Status IS NULL OR m.status = @Status::common_status)
            """;

        var effectiveAllowedTenantIds =
            allowedTenantIds is { Count: > 0 } ? allowedTenantIds.ToArray() : null;

        if (effectiveAllowedTenantIds is not null)
            sql += " AND m.tenant_id = ANY(@AllowedTenantIds)";

        sql += " ORDER BY m.created_at DESC LIMIT @PageSize OFFSET @Offset";

        await using var conn = new NpgsqlConnection(_connectionString);
        var results = await conn.QueryAsync<SubscriptionDto>(
            new CommandDefinition(sql, new
            {
                SchoolId = schoolId,
                TenantId = tenantId,
                Status = status,
                PageSize = pageSize,
                Offset = (page - 1) * pageSize,
                AllowedTenantIds = effectiveAllowedTenantIds
            }, cancellationToken: ct));

        return results.AsList();
    }

    public async Task<int> CountSubscriptionsAsync(
        Guid? schoolId,
        Guid? tenantId,
        string? status,
        IReadOnlyList<Guid>? allowedTenantIds,
        CancellationToken ct = default)
    {
        var sql = """
            SELECT COUNT(1)
            FROM school_tenant_mapping m
            WHERE m.is_deleted = FALSE
              AND (@SchoolId IS NULL OR m.school_id = @SchoolId)
              AND (@TenantId IS NULL OR m.tenant_id = @TenantId)
              AND (@Status IS NULL OR m.status = @Status::common_status)
            """;

        var effectiveAllowedTenantIds =
            allowedTenantIds is { Count: > 0 } ? allowedTenantIds.ToArray() : null;

        if (effectiveAllowedTenantIds is not null)
            sql += " AND m.tenant_id = ANY(@AllowedTenantIds)";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new
            {
                SchoolId = schoolId,
                TenantId = tenantId,
                Status = status,
                AllowedTenantIds = effectiveAllowedTenantIds
            }, cancellationToken: ct));
    }

    public async Task<int> CountSubscriptionsExpiringSoonAsync(Guid tenantId, DateTime currentTime, DateTime threshold, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM school_tenant_mapping m
            WHERE m.is_deleted = FALSE
              AND m.tenant_id = @TenantId
              AND m.status = 'ACTIVE'::common_status
              AND m.contract_end IS NOT NULL
              AND m.contract_end > @CurrentTime
              AND m.contract_end <= @Threshold
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new
        {
            TenantId = tenantId,
            CurrentTime = currentTime,
            Threshold = threshold
        }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Guid>> GetTenantAdminTenantIdsAsync(Guid userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT DISTINCT a.tenant_id
            FROM user_tenant_role_assignment a
            INNER JOIN role r ON r.id = a.role_id
            WHERE a.user_id = @UserId
              AND a.is_deleted = FALSE
              AND a.tenant_id IS NOT NULL
              AND r.code = 'TENANT_ADMIN'
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var tenantIds = await conn.QueryAsync<Guid>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));

        return tenantIds.AsList();
    }

    public async Task<bool> SubscriptionExistsAsync(
        Guid schoolId, Guid tenantId, Guid? excludeId, CancellationToken ct = default)
    {
        var sql = """
            SELECT COUNT(1) FROM school_tenant_mapping
            WHERE school_id = @SchoolId AND tenant_id = @TenantId AND is_deleted = FALSE
            """;
        if (excludeId.HasValue)
            sql += " AND id != @ExcludeId";

        await using var conn = new NpgsqlConnection(_connectionString);
        var count = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { SchoolId = schoolId, TenantId = tenantId, ExcludeId = excludeId },
                cancellationToken: ct));
        return count > 0;
    }

    public async Task CreateSubscriptionAsync(SchoolSubscription subscription, CancellationToken ct = default)
    {
        const string insertMappingSql = """
            INSERT INTO school_tenant_mapping (
                id, tenant_id, school_id,
                contract_start, contract_end,
                max_concurrent_sessions, login_policy,
                enforce_expiry, status, created_at, updated_at)
            VALUES (
                @Id, @TenantId, @SchoolId,
                @ContractStart, @ContractEnd,
                @MaxConcurrentSessions, @LoginPolicy::login_policy,
                @EnforceExpiry, @Status::common_status, NOW(), NOW())
            """;

        const string insertAssignmentsSql = """
            INSERT INTO user_tenant_role_assignment (id, user_id, tenant_id, role_id, is_inherited, created_at)
            SELECT gen_random_uuid(), ua.id, @TenantId, r.id, TRUE, NOW()
            FROM user_account ua
            JOIN role r ON r.code = 'SCHOOL'
            WHERE ua.home_school_id = @SchoolId
                AND ua.is_deleted = FALSE
                AND ua.account_type::TEXT = 'SCHOOL'
                AND r.is_deleted = FALSE
                AND NOT EXISTS (
                        SELECT 1 FROM user_tenant_role_assignment x
                        WHERE x.user_id = ua.id
                            AND x.tenant_id = @TenantId
                            AND x.role_id = r.id
                            AND x.is_deleted = FALSE
                )
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            await conn.ExecuteAsync(new CommandDefinition(insertMappingSql, new
            {
                subscription.Id, subscription.TenantId, subscription.SchoolId,
                subscription.ContractStart, subscription.ContractEnd,
                subscription.MaxConcurrentSessions, subscription.LoginPolicy,
                subscription.EnforceExpiry, subscription.Status
            }, transaction: tx, cancellationToken: ct));

            await conn.ExecuteAsync(new CommandDefinition(insertAssignmentsSql, new
            {
                TenantId = subscription.TenantId,
                SchoolId = subscription.SchoolId
            }, transaction: tx, cancellationToken: ct));

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<UpdateSubscriptionResult?> UpdateSubscriptionAsync(
        UpdateSubscriptionCommand command, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE school_tenant_mapping
            SET contract_start          = @ContractStart,
                contract_end            = @ContractEnd,
                max_concurrent_sessions = @MaxConcurrentSessions,
                login_policy            = @LoginPolicy::login_policy,
                enforce_expiry          = @EnforceExpiry,
                updated_at              = NOW()
            WHERE id = @Id AND school_id = @SchoolId AND is_deleted = FALSE
            RETURNING id AS Id, school_id AS SchoolId, tenant_id AS TenantId,
                      contract_start AS ContractStart, contract_end AS ContractEnd,
                      max_concurrent_sessions AS MaxConcurrentSessions,
                      login_policy::TEXT AS LoginPolicy,
                      enforce_expiry AS EnforceExpiry
            """;
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<UpdateSubscriptionResult>(
            new CommandDefinition(sql, new
            {
                command.Id, command.SchoolId,
                command.ContractStart, command.ContractEnd,
                command.MaxConcurrentSessions, command.LoginPolicy, command.EnforceExpiry
            }, cancellationToken: ct));
    }

    public async Task<bool> DeleteSubscriptionAsync(Guid id, Guid schoolId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE school_tenant_mapping
            SET is_deleted = TRUE, updated_at = NOW()
            WHERE id = @Id AND school_id = @SchoolId AND is_deleted = FALSE
            """;
        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id, SchoolId = schoolId }, cancellationToken: ct));
        return rows > 0;
    }
}
