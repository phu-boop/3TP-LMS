namespace Aig.Lms.BuildingBlocks.Domain.Emails;

public enum EmailOutboxMessageStatus
{
    Pending = 0,
    Processing = 1,
    Sent = 2,
    Failed = 3
}
