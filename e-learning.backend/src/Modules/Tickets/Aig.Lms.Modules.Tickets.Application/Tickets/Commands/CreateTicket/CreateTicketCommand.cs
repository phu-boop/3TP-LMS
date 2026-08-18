using System;
using System.Collections.Generic;
using Aig.Lms.Modules.Tickets.Application.Tickets.Dtos;

namespace Aig.Lms.Modules.Tickets.Application.Tickets.Commands.CreateTicket;

public sealed record CreateTicketCommand(
    Guid TenantId,
    Guid SchoolId,
    Guid CreatorId,
    string Title,
    string Description,
    List<string>? FileNames);

public sealed record CreateTicketResult(
    Guid TicketId,
    List<AttachmentUploadSessionDto> UploadSessions);
