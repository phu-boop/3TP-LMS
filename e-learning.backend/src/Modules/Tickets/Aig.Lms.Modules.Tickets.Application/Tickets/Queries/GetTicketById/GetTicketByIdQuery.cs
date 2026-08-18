using Aig.Lms.Modules.Tickets.Application.Abstractions;
using Aig.Lms.Modules.Tickets.Application.Tickets.Dtos;

namespace Aig.Lms.Modules.Tickets.Application.Tickets.Queries;

public sealed record GetTicketByIdQuery(Guid Id);

public sealed class GetTicketByIdHandler
{
    private readonly ITicketReadRepository _readRepository;

    public GetTicketByIdHandler(ITicketReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<TicketDto?> HandleAsync(GetTicketByIdQuery query, CancellationToken ct = default) =>
        await _readRepository.GetByIdAsync(query.Id, ct);
}
