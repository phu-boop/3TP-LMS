using Aig.Lms.Modules.Tickets.Application.Abstractions;
using Aig.Lms.Modules.Tickets.Application.Tickets.Dtos;

namespace Aig.Lms.Modules.Tickets.Application.Tickets.Queries;

public sealed record GetTicketDetailQuery(Guid TicketId, Guid CreatorId, Guid TenantId);

public sealed class GetTicketDetailHandler
{
    private readonly ITicketReadRepository _readRepository;

    public GetTicketDetailHandler(ITicketReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<TicketDetailDto?> HandleAsync(GetTicketDetailQuery query, CancellationToken ct = default)
    {
        return await _readRepository.GetDetailAsync(query.TicketId, query.CreatorId, query.TenantId, ct);
    }
}
