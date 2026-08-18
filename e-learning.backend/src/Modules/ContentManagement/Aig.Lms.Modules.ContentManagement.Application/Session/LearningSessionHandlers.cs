using System.Threading;
using System.Threading.Tasks;
using Aig.Lms.Modules.ContentManagement.Application.Content;

namespace Aig.Lms.Modules.ContentManagement.Application.Session;

public sealed class StartSessionCommandHandler(ILearningSessionRepository repository)
{
    public Task<LearningSessionDto> HandleAsync(StartSessionCommand command, CancellationToken ct = default)
        => repository.StartSessionAsync(command, ct);
}

public sealed class HeartbeatCommandHandler(ILearningSessionRepository repository)
{
    public Task HandleAsync(HeartbeatCommand command, CancellationToken ct = default)
        => repository.UpdateHeartbeatAsync(command, ct);
}

public sealed class EndSessionCommandHandler(ILearningSessionRepository repository)
{
    public Task<SessionSummaryDto> HandleAsync(EndSessionCommand command, CancellationToken ct = default)
        => repository.EndSessionAsync(command, ct);
}

public sealed class GetSessionReportQueryHandler(ILearningSessionRepository repository)
{
    public Task<PagedResult<SessionReportDto>> HandleAsync(SessionReportQuery query, CancellationToken ct = default)
        => repository.GetReportAsync(query, ct);
}
