using Aig.Lms.Modules.Tickets.Application.Abstractions;
using Aig.Lms.Modules.Tickets.Application.Tickets.Dtos;

namespace Aig.Lms.Modules.Tickets.Application.Tickets.Queries;

public sealed record GetMyTicketsQuery(Guid CreatorId, Guid TenantId, int Page, int PageSize, string? Status, string? Search);

public sealed record GetMyTicketsResult(
    IReadOnlyList<MyTicketDto> Items,
    int Total,
    int Page,
    int PageSize);

public sealed class GetMyTicketsHandler
{
    private readonly ITicketReadRepository _readRepository;

    public GetMyTicketsHandler(ITicketReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<GetMyTicketsResult> HandleAsync(GetMyTicketsQuery query, CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize > 100 ? 100 : query.PageSize;

        var items = await _readRepository.ListByCreatorAsync(query.CreatorId, query.TenantId, page, pageSize, query.Status, query.Search, ct);
        var total = await _readRepository.CountByCreatorAsync(query.CreatorId, query.TenantId, query.Status, query.Search, ct);

        return new GetMyTicketsResult(items, total, page, pageSize);
    }
}
