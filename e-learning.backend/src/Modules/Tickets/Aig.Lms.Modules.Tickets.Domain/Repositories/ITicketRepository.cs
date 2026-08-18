using Aig.Lms.Modules.Tickets.Domain.Entities;

namespace Aig.Lms.Modules.Tickets.Domain.Repositories;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Ticket ticket, CancellationToken ct = default);
    Task UpdateAsync(Ticket ticket, CancellationToken ct = default);
    Task AddAttachmentAsync(TicketAttachment attachment, CancellationToken ct = default);
    Task UpdateAttachmentAsync(TicketAttachment attachment, CancellationToken ct = default);
    Task<IReadOnlyList<Ticket>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<(IReadOnlyList<Ticket> Items, int Total)> GetByTenantIdPagedAsync(Guid tenantId, int page, int pageSize, string? search = null, CancellationToken ct = default);
}
