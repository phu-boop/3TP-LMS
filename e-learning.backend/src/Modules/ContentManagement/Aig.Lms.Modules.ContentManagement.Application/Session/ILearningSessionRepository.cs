using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aig.Lms.Modules.ContentManagement.Application.Content;

namespace Aig.Lms.Modules.ContentManagement.Application.Session;

public interface ILearningSessionRepository
{
    Task<LearningSessionDto> StartSessionAsync(StartSessionCommand command, CancellationToken ct = default);
    Task UpdateHeartbeatAsync(HeartbeatCommand command, CancellationToken ct = default);
    Task<SessionSummaryDto> EndSessionAsync(EndSessionCommand command, CancellationToken ct = default);
    Task<PagedResult<SessionReportDto>> GetReportAsync(SessionReportQuery query, CancellationToken ct = default);
}
