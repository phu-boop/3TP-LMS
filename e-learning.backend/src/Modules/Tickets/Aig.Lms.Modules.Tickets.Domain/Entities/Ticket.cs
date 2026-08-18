using Aig.Lms.BuildingBlocks.Domain.Entities;
using Aig.Lms.BuildingBlocks.Domain.Exceptions;
using Aig.Lms.Modules.Tickets.Domain.Events;

namespace Aig.Lms.Modules.Tickets.Domain.Entities;

public sealed class Ticket : AggregateRoot<Guid>
{
    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid CreatorId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Status { get; private set; } = TicketStatus.New;
    public string? ResolutionNote { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime UpdatedAt { get; private set; }
    public IReadOnlyCollection<TicketAttachment> Attachments => _attachments.AsReadOnly();

    private readonly List<TicketAttachment> _attachments = [];

    private Ticket() { }

    private Ticket(
        Guid id,
        Guid tenantId,
        Guid schoolId,
        Guid creatorId,
        string title,
        string description,
        string status,
        string? resolutionNote,
        DateTime createdAt,
        DateTime updatedAt,
        List<TicketAttachment>? attachments = null)
    {
        Id = id;
        TenantId = tenantId;
        SchoolId = schoolId;
        CreatorId = creatorId;
        Title = NormalizeTitle(title);
        Description = NormalizeDescription(description);
        Status = TicketStatus.Normalize(status);
        ResolutionNote = NormalizeOptional(resolutionNote);
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        if (attachments is not null)
            _attachments = attachments;
    }

    public static Ticket Create(
        Guid tenantId,
        Guid schoolId,
        Guid creatorId,
        string title,
        string description,
        List<TicketAttachment>? attachments = null)
    {
        var ticket = new Ticket(
            Guid.NewGuid(),
            tenantId,
            schoolId,
            creatorId,
            title,
            description,
            TicketStatus.New,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow,
            attachments);

        ticket.RaiseDomainEvent(new TicketCreatedDomainEvent(ticket.Id, ticket.TenantId, ticket.SchoolId, ticket.Title));
        return ticket;
    }

    public static Ticket Reconstitute(
        Guid id,
        Guid tenantId,
        Guid schoolId,
        Guid creatorId,
        string title,
        string description,
        string status,
        string? resolutionNote,
        DateTime createdAt,
        DateTime updatedAt,
        List<TicketAttachment>? attachments = null) =>
        new(
            id,
            tenantId,
            schoolId,
            creatorId,
            title,
            description,
            status,
            resolutionNote,
            createdAt,
            updatedAt,
            attachments);

    public void AddAttachment(TicketAttachment attachment)
    {
        _attachments.Add(attachment);
    }

    public void MarkInProgress()
    {
        if (Status != TicketStatus.New)
            throw new DomainException($"Cannot change status to In Progress from '{Status}'. Only New tickets can be processed.");

        Status = TicketStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Resolve(string resolutionNote)
    {
        if (Status != TicketStatus.InProgress)
            throw new DomainException($"Cannot resolve ticket from status '{Status}'. Ticket must be In Progress first.");

        if (string.IsNullOrWhiteSpace(resolutionNote))
            throw new DomainException("Resolution note is required when resolving a ticket.");

        ResolutionNote = resolutionNote.Trim();
        Status = TicketStatus.Resolved;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new TicketResolvedDomainEvent(Id, TenantId, SchoolId, Title, ResolutionNote));
    }

    public void Close(string note)
    {
        if (Status == TicketStatus.Resolved || Status == TicketStatus.Closed)
            throw new DomainException("Cannot close a resolved or closed ticket.");

        if (string.IsNullOrWhiteSpace(note))
            throw new DomainException("Closing note is required.");

        ResolutionNote = note.Trim();
        Status = TicketStatus.Closed;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Ticket title cannot be empty.");

        var normalized = title.Trim();
        if (normalized.Length > 255)
            throw new DomainException("Ticket title must not exceed 255 characters.");

        return normalized;
    }

    private static string NormalizeDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Ticket description cannot be empty.");

        return description.Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
