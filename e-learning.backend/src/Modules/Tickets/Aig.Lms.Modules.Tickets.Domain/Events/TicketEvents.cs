using Aig.Lms.BuildingBlocks.Domain.Abstractions;

namespace Aig.Lms.Modules.Tickets.Domain.Events;

public sealed record TicketCreatedDomainEvent(Guid TicketId, Guid TenantId, Guid SchoolId, string Title) : IDomainEvent;

public sealed record TicketResolvedDomainEvent(Guid TicketId, Guid TenantId, Guid SchoolId, string Title, string ResolutionNote) : IDomainEvent;
