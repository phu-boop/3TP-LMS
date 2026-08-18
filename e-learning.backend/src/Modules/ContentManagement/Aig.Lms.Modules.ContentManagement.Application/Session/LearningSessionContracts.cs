using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Aig.Lms.Modules.ContentManagement.Application.Session;

public sealed record StartSessionCommand(
    Guid TenantId,
    Guid UserId,
    Guid? SchoolId,
    Guid LearningSessionId,
    Guid SessionId,
    Guid ContentItemId,
    JsonDocument? DeviceInfo,
    string? IpAddress);

public sealed record HeartbeatCommand(
    Guid TenantId,
    Guid UserId,
    Guid LearningSessionId);

public sealed record EndSessionCommand(
    Guid TenantId,
    Guid UserId,
    Guid LearningSessionId);

public sealed record SessionReportQuery(
    Guid TenantId,
    Guid? UserId,
    Guid? SchoolId,
    Guid? ContentItemId,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 20);

public sealed record LearningSessionDto(
    Guid Id,
    Guid LearningSessionId,
    Guid SessionId,
    DateTime StartAt);

public sealed record SessionSummaryDto(
    Guid LearningSessionId,
    long TotalMs,
    long InactivityMs,
    long ActiveMs);

public sealed record SessionReportDto(
    Guid TenantId,
    Guid UserId,
    Guid? SchoolId,
    Guid? ContentItemId,
    int SessionCount,
    long TotalSessionMs,
    long TotalInactivityMs,
    long ActiveLearningMs);
