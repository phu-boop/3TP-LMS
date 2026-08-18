using Aig.Lms.Modules.Tickets.Application.Abstractions;
using Aig.Lms.Modules.Tickets.Application.Tickets.Dtos;

namespace Aig.Lms.Modules.Tickets.Application.Tickets.Queries;

public sealed record GetTicketsQuery(Guid TenantId, int Page, int PageSize, string? Search, string? Status);

public sealed record GetTicketsResult(
    IReadOnlyList<TicketListItemDto> Items,
    int Total,
    int Page,
    int PageSize);

public sealed class GetTicketsQueryHandler
{
    private readonly ITicketReadRepository _readRepository;

    public GetTicketsQueryHandler(ITicketReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<GetTicketsResult> HandleAsync(GetTicketsQuery query, CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize > 100 ? 100 : query.PageSize;

        var items = await _readRepository.ListByTenantIdAsync(query.TenantId, page, pageSize, query.Search, query.Status, ct);
        var total = await _readRepository.CountByTenantIdAsync(query.TenantId, query.Search, query.Status, ct);

        return new GetTicketsResult(items, total, page, pageSize);
    }
}