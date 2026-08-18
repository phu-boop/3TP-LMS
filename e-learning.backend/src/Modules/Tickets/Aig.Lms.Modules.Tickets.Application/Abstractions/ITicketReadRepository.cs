using Aig.Lms.Modules.Tickets.Application.Tickets.Dtos;

namespace Aig.Lms.Modules.Tickets.Application.Abstractions;

public interface ITicketReadRepository
{
    Task<IReadOnlyList<MyTicketDto>> ListByCreatorAsync(Guid creatorId, Guid tenantId, int page, int pageSize, string? status, string? search, CancellationToken ct = default);
    Task<int> CountByCreatorAsync(Guid creatorId, Guid tenantId, string? status, string? search, CancellationToken ct = default);
    Task<TicketDetailDto?> GetDetailAsync(Guid ticketId, Guid? creatorId = null, Guid? tenantId = null, CancellationToken ct = default);
    Task<TicketDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TicketListItemDto>> ListByTenantIdAsync(
        Guid tenantId,
        int page,
        int pageSize,
        string? search,
        string? status,
        CancellationToken ct = default);
    Task<int> CountByTenantIdAsync(Guid tenantId, string? search, string? status, CancellationToken ct = default);
}
