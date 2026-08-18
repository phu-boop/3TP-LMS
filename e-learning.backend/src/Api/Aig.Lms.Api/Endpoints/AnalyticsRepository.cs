using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Aig.Lms.Api.Endpoints;

public interface IAnalyticsRepository
{
    Task<AnalyticsSummaryDto> GetSummaryAsync(Guid? tenantId, DateTime fromDate, DateTime toDate, string? provinceName, CancellationToken ct = default);
    Task<IReadOnlyList<RegionalAnalyticsDto>> GetRegionalAnalyticsAsync(Guid? tenantId, DateTime fromDate, DateTime toDate, string? provinceName, CancellationToken ct = default);
    Task<IReadOnlyList<TopSchoolAnalyticsDto>> GetTopSchoolsAsync(Guid? tenantId, DateTime fromDate, DateTime toDate, string? provinceName, CancellationToken ct = default);
    Task<IReadOnlyList<TopTenantAnalyticsDto>> GetTopTenantsAsync(Guid? tenantId, DateTime fromDate, DateTime toDate, string? provinceName, CancellationToken ct = default);
    Task<IReadOnlyList<TrendAnalyticsDto>> GetTrendAsync(Guid? tenantId, DateTime fromDate, DateTime toDate, string? provinceName, string groupBy, CancellationToken ct = default);
}

public sealed class AnalyticsRepository : IAnalyticsRepository
{
    private readonly string _connectionString;

    public AnalyticsRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");
    }

    public async Task<AnalyticsSummaryDto> GetSummaryAsync(Guid? tenantId, DateTime fromDate, DateTime toDate, string? provinceName, CancellationToken ct = default)
    {
        const string batchSql = """
            -- total_learners
            SELECT COUNT(DISTINCT uls.user_id) FROM user_learning_session uls
            JOIN school s ON s.id = uls.school_id
            LEFT JOIN provinces p ON p.name = s.province_code
            WHERE (@TenantId IS NULL OR uls.tenant_id = @TenantId)
              AND uls.is_deleted = FALSE
              AND uls.last_update >= @FromDate
              AND uls.last_update < @ToDate
              AND (@ProvinceName IS NULL OR @ProvinceName = '' OR p.name = @ProvinceName);

            -- one_year_summary
            SELECT
                DATE_TRUNC('month', uls.last_update) AS MonthStart,
                SUM(
                    EXTRACT(EPOCH FROM (uls.last_update - uls.start_at))
                    - (uls.inactivity_ms / 1000.0)
                ) AS TotalActiveSeconds
            FROM user_learning_session uls
            JOIN school s ON s.id = uls.school_id
            LEFT JOIN provinces p ON p.name = s.province_code
            WHERE (@TenantId IS NULL OR uls.tenant_id = @TenantId)
              AND uls.is_deleted = FALSE
              AND uls.last_update >= @FromDate
              AND uls.last_update < @ToDate
              AND (@ProvinceName IS NULL OR @ProvinceName = '' OR p.name = @ProvinceName)
            GROUP BY MonthStart
            ORDER BY MonthStart;

            -- avg_study_hours_day
            WITH user_daily_seconds AS (
                SELECT
                    uls.user_id,
                    DATE(uls.last_update AT TIME ZONE 'Asia/Ho_Chi_Minh') AS learning_date,
                    GREATEST(0, SUM(EXTRACT(EPOCH FROM (uls.last_update - uls.start_at)) - (uls.inactivity_ms / 1000.0))) AS active_seconds
                FROM user_learning_session uls
                JOIN school s ON s.id = uls.school_id
                LEFT JOIN provinces p ON p.name = s.province_code
                WHERE (@TenantId IS NULL OR uls.tenant_id = @TenantId)
                  AND uls.is_deleted = FALSE
                  AND uls.last_update >= @FromDate
                  AND uls.last_update < @ToDate
                  AND (@ProvinceName IS NULL OR @ProvinceName = '' OR p.name = @ProvinceName)
                GROUP BY uls.user_id, learning_date
            )
            SELECT COALESCE(AVG(active_seconds) / 3600.0, 0) FROM user_daily_seconds;

            -- avg_study_hours_week
            WITH user_weekly_seconds AS (
                SELECT
                    uls.user_id,
                    DATE_TRUNC('week', uls.last_update) AS learning_week,
                    GREATEST(0, SUM(EXTRACT(EPOCH FROM (uls.last_update - uls.start_at)) - (uls.inactivity_ms / 1000.0))) AS active_seconds
                FROM user_learning_session uls
                JOIN school s ON s.id = uls.school_id
                LEFT JOIN provinces p ON p.name = s.province_code
                WHERE (@TenantId IS NULL OR uls.tenant_id = @TenantId)
                  AND uls.is_deleted = FALSE
                  AND uls.last_update >= @FromDate
                  AND uls.last_update < @ToDate
                  AND (@ProvinceName IS NULL OR @ProvinceName = '' OR p.name = @ProvinceName)
                GROUP BY uls.user_id, learning_week
            )
            SELECT COALESCE(AVG(active_seconds) / 3600.0, 0) FROM user_weekly_seconds;

            -- avg_study_hours_month
            WITH user_monthly_seconds AS (
                SELECT
                    uls.user_id,
                    DATE_TRUNC('month', uls.last_update) AS learning_month,
                    GREATEST(0, SUM(EXTRACT(EPOCH FROM (uls.last_update - uls.start_at)) - (uls.inactivity_ms / 1000.0))) AS active_seconds
                FROM user_learning_session uls
                JOIN school s ON s.id = uls.school_id
                LEFT JOIN provinces p ON p.name = s.province_code
                WHERE (@TenantId IS NULL OR uls.tenant_id = @TenantId)
                  AND uls.is_deleted = FALSE
                  AND uls.last_update >= @FromDate
                  AND uls.last_update < @ToDate
                  AND (@ProvinceName IS NULL OR @ProvinceName = '' OR p.name = @ProvinceName)
                GROUP BY uls.user_id, learning_month
            )
            SELECT COALESCE(AVG(active_seconds) / 3600.0, 0) FROM user_monthly_seconds;
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await using var cmd = new NpgsqlCommand(batchSql, conn)
        {
            Parameters =
            {
                new NpgsqlParameter("TenantId", NpgsqlTypes.NpgsqlDbType.Uuid) { Value = (object?)tenantId ?? DBNull.Value },
                new NpgsqlParameter("FromDate", NpgsqlTypes.NpgsqlDbType.TimestampTz) { Value = fromDate },
                new NpgsqlParameter("ToDate", NpgsqlTypes.NpgsqlDbType.TimestampTz) { Value = toDate },
                new NpgsqlParameter("ProvinceName", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = (object?)provinceName ?? DBNull.Value },
            }
        };

        await conn.OpenAsync(ct);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        // Result set 1: total learners
        var totalLearners = 0L;
        if (await reader.ReadAsync(ct))
            totalLearners = reader.GetInt64(0);

        // Advance to next result set
        await reader.NextResultAsync(ct);

        // Result set 2: one-year summary
        var oneYearSummary = new List<MonthlyStudyTimeDto>();
        while (await reader.ReadAsync(ct))
        {
            oneYearSummary.Add(new MonthlyStudyTimeDto(
                reader.GetDateTime(0),
                reader.GetDecimal(1)));
        }

        // Advance to next result set
        await reader.NextResultAsync(ct);

        // Result set 3: average study hours day
        var avgStudyHoursDay = 0m;
        if (await reader.ReadAsync(ct))
            avgStudyHoursDay = reader.GetDecimal(0);

        // Advance to next result set
        await reader.NextResultAsync(ct);

        // Result set 4: average study hours week
        var avgStudyHoursWeek = 0m;
        if (await reader.ReadAsync(ct))
            avgStudyHoursWeek = reader.GetDecimal(0);

        // Advance to next result set
        await reader.NextResultAsync(ct);

        // Result set 5: average study hours month
        var avgStudyHoursMonth = 0m;
        if (await reader.ReadAsync(ct))
            avgStudyHoursMonth = reader.GetDecimal(0);

        return new AnalyticsSummaryDto(totalLearners, oneYearSummary, avgStudyHoursDay, avgStudyHoursWeek, avgStudyHoursMonth);
    }

    public async Task<IReadOnlyList<RegionalAnalyticsDto>> GetRegionalAnalyticsAsync(Guid? tenantId, DateTime fromDate, DateTime toDate, string? provinceName, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                CASE 
                    WHEN @ProvinceName IS NULL OR @ProvinceName = '' THEN COALESCE(p.name, s.province_code, 'Chưa xác định')
                    ELSE COALESCE(w.name, s.district_code, 'Chưa xác định')
                END AS RegionName,
                COUNT(DISTINCT uls.user_id) AS LearnerCount,
                ROUND(GREATEST(0, SUM(
                    EXTRACT(EPOCH FROM (uls.last_update - uls.start_at))
                    - (uls.inactivity_ms / 1000.0)
                )::numeric / 3600.0), 2) AS TotalStudyHours
            FROM user_learning_session uls
            JOIN school s ON s.id = uls.school_id
            LEFT JOIN provinces p ON p.name = s.province_code
            LEFT JOIN wards w ON w.province_id = p.id AND w.name = s.district_code
            WHERE (@TenantId IS NULL OR uls.tenant_id = @TenantId)
              AND uls.is_deleted = FALSE
              AND uls.last_update >= @FromDate
              AND uls.last_update < @ToDate
              AND (@ProvinceName IS NULL OR @ProvinceName = '' OR p.name = @ProvinceName)
            GROUP BY RegionName
            ORDER BY TotalStudyHours DESC;
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var result = await conn.QueryAsync<RegionalAnalyticsDto>(new CommandDefinition(sql, new { TenantId = tenantId, FromDate = fromDate, ToDate = toDate, ProvinceName = provinceName }, cancellationToken: ct));
        return result.ToList();
    }

    public async Task<IReadOnlyList<TopSchoolAnalyticsDto>> GetTopSchoolsAsync(Guid? tenantId, DateTime fromDate, DateTime toDate, string? provinceName, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                s.id AS SchoolId,
                s.name AS SchoolName,
                COALESCE(p.name, s.province_code, 'Chưa xác định') AS Region,
                ROUND((GREATEST(0, SUM(EXTRACT(EPOCH FROM (uls.last_update - uls.start_at)) - (uls.inactivity_ms / 1000.0))) / 3600.0 / GREATEST(1, EXTRACT(EPOCH FROM (@ToDate - @FromDate)) / 86400))::numeric, 2) AS AvgStudyHoursPerDay
            FROM school s
            JOIN user_learning_session uls ON uls.school_id = s.id
            LEFT JOIN provinces p ON p.name = s.province_code
            WHERE uls.is_deleted = FALSE
              AND uls.last_update >= @FromDate
              AND uls.last_update < @ToDate
              AND (@ProvinceName IS NULL OR @ProvinceName = '' OR p.name = @ProvinceName)
              AND (@TenantId IS NULL OR EXISTS (
                  SELECT 1 FROM school_tenant_mapping stm
                  WHERE stm.school_id = s.id AND stm.tenant_id = @TenantId
              ))
            GROUP BY s.id, s.name, p.name, s.province_code
            ORDER BY AvgStudyHoursPerDay DESC
            LIMIT 5;
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var result = await conn.QueryAsync<TopSchoolAnalyticsDto>(new CommandDefinition(sql, new { TenantId = tenantId, FromDate = fromDate, ToDate = toDate, ProvinceName = provinceName }, cancellationToken: ct));
        return result.ToList();
    }

    public async Task<IReadOnlyList<TopTenantAnalyticsDto>> GetTopTenantsAsync(Guid? tenantId, DateTime fromDate, DateTime toDate, string? provinceName, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                t.id AS TenantId,
                t.name AS TenantName,
                ROUND((GREATEST(0, SUM(EXTRACT(EPOCH FROM (uls.last_update - uls.start_at)) - (uls.inactivity_ms / 1000.0))) / 3600.0 / GREATEST(1, EXTRACT(EPOCH FROM (@ToDate - @FromDate)) / 86400))::numeric, 2) AS AvgStudyHoursPerDay
            FROM tenant t
            JOIN user_learning_session uls ON uls.tenant_id = t.id
            JOIN school s ON s.id = uls.school_id
            LEFT JOIN provinces p ON p.name = s.province_code
            WHERE uls.is_deleted = FALSE
              AND uls.last_update >= @FromDate
              AND uls.last_update < @ToDate
              AND (@ProvinceName IS NULL OR @ProvinceName = '' OR p.name = @ProvinceName)
              AND (@TenantId IS NULL OR t.id = @TenantId)
            GROUP BY t.id, t.name
            ORDER BY AvgStudyHoursPerDay DESC
            LIMIT 5;
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var result = await conn.QueryAsync<TopTenantAnalyticsDto>(new CommandDefinition(sql, new { TenantId = tenantId, FromDate = fromDate, ToDate = toDate, ProvinceName = provinceName }, cancellationToken: ct));
        return result.ToList();
    }

    public async Task<IReadOnlyList<TrendAnalyticsDto>> GetTrendAsync(Guid? tenantId, DateTime fromDate, DateTime toDate, string? provinceName, string groupBy, CancellationToken ct = default)
    {
        var sql = $"""
            SELECT
                DATE_TRUNC('{groupBy}', uls.last_update AT TIME ZONE 'Asia/Ho_Chi_Minh') AS TimeSlot,
                COUNT(DISTINCT uls.user_id) AS LearnerCount,
                ROUND(GREATEST(0, SUM(
                    EXTRACT(EPOCH FROM (uls.last_update - uls.start_at))
                    - (uls.inactivity_ms / 1000.0)
                )::numeric / 3600.0), 2) AS TotalStudyHours
            FROM user_learning_session uls
            JOIN school s ON s.id = uls.school_id
            LEFT JOIN provinces p ON p.name = s.province_code
            WHERE (@TenantId IS NULL OR uls.tenant_id = @TenantId)
              AND uls.is_deleted = FALSE
              AND uls.last_update >= @FromDate
              AND uls.last_update < @ToDate
              AND (@ProvinceName IS NULL OR @ProvinceName = '' OR p.name = @ProvinceName)
            GROUP BY TimeSlot
            ORDER BY TimeSlot;
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var result = await conn.QueryAsync<TrendAnalyticsDto>(new CommandDefinition(sql, new { TenantId = tenantId, FromDate = fromDate, ToDate = toDate, ProvinceName = provinceName }, cancellationToken: ct));
        return result.ToList();
    }
}

public sealed record AnalyticsSummaryDto(
    long TotalLearners,
    IReadOnlyList<MonthlyStudyTimeDto> OneYearSummary,
    decimal AvgStudyHoursDay,
    decimal AvgStudyHoursWeek,
    decimal AvgStudyHoursMonth);

public sealed record MonthlyStudyTimeDto(
    DateTime MonthStart,
    decimal TotalActiveSeconds);

public sealed record RegionalAnalyticsDto(
    string RegionName,
    long LearnerCount,
    decimal TotalStudyHours);

public sealed record TrendAnalyticsDto(
    DateTime TimeSlot,
    long LearnerCount,
    decimal TotalStudyHours);

public sealed record TopTenantAnalyticsDto(
    Guid TenantId,
    string TenantName,
    decimal AvgStudyHoursPerDay);

public sealed record TopSchoolAnalyticsDto(
    Guid SchoolId,
    string SchoolName,
    string Region,
    decimal AvgStudyHoursPerDay);
