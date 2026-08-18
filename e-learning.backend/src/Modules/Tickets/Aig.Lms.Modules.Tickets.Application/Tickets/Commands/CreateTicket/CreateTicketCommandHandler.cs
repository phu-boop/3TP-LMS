using Aig.Lms.BuildingBlocks.Application.Abstractions;
using Aig.Lms.Modules.Tickets.Application.Tickets.Dtos;
using Aig.Lms.Modules.Tickets.Domain.Entities;
using Aig.Lms.Modules.Tickets.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Aig.Lms.Modules.Tickets.Application.Tickets.Commands.CreateTicket;

public sealed class CreateTicketCommandHandler
{
    private readonly ITicketRepository _repository;
    private readonly IObjectStorage _objectStorage;
    private readonly IConfiguration _configuration;

    public CreateTicketCommandHandler(
        ITicketRepository repository,
        IObjectStorage objectStorage,
        IConfiguration configuration)
    {
        _repository = repository;
        _objectStorage = objectStorage;
        _configuration = configuration;
    }

    public async Task<CreateTicketResult> HandleAsync(CreateTicketCommand command, CancellationToken ct = default)
    {
        var ticket = Ticket.Create(
            command.TenantId,
            command.SchoolId,
            command.CreatorId,
            command.Title,
            command.Description);

        var uploadSessions = new List<AttachmentUploadSessionDto>();
        var bucket = _configuration["ObjectStorage:ContentBucket"] ?? "lms-content";
        var ttl = TimeSpan.FromHours(1);

        if (command.FileNames != null && command.FileNames.Count > 0)
        {
            foreach (var fileName in command.FileNames)
            {
                var safeFileName = Path.GetFileName(fileName);
                var attachmentId = Guid.NewGuid();
                var objectKey = $"tenants/{command.TenantId}/tickets/{ticket.Id}/attachments/{attachmentId}/{safeFileName}";

                var attachment = TicketAttachment.Reconstitute(
                    attachmentId,
                    ticket.Id,
                    safeFileName,
                    objectKey,
                    "application/octet-stream",
                    0,
                    DateTime.UtcNow);

                ticket.AddAttachment(attachment);

                var uploadUrl = await _objectStorage.GetPresignedPutUrlAsync(bucket, objectKey, ttl, ct);

                uploadSessions.Add(new AttachmentUploadSessionDto(
                    attachmentId,
                    safeFileName,
                    uploadUrl,
                    objectKey,
                    DateTimeOffset.UtcNow.Add(ttl)));
            }
        }

        await _repository.AddAsync(ticket, ct);

        return new CreateTicketResult(ticket.Id, uploadSessions);
    }
}
