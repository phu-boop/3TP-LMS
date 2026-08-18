using Aig.Lms.BuildingBlocks.Domain.Exceptions;

namespace Aig.Lms.Modules.Tickets.Domain.Entities;

public static class TicketStatus
{
    public const string New = "NEW";
    public const string InProgress = "IN_PROGRESS";
    public const string Resolved = "RESOLVED";
    public const string Closed = "CLOSED";

    public static readonly IReadOnlySet<string> Allowed = new HashSet<string>(StringComparer.Ordinal)
    {
        New,
        InProgress,
        Resolved,
        Closed,
    };

    public static string Normalize(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new DomainException("Ticket status is required.");

        var normalized = status.Trim().ToUpperInvariant();
        if (!Allowed.Contains(normalized))
            throw new DomainException($"Unsupported ticket status '{status}'.");

        return normalized;
    }
}
