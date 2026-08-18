using System;

namespace Aig.Lms.Modules.Tickets.Application.Tickets.Commands.ConfirmTicketUpload;

public sealed record ConfirmTicketUploadCommand(
    Guid TenantId,
    Guid TicketId,
    Guid ActorId);
