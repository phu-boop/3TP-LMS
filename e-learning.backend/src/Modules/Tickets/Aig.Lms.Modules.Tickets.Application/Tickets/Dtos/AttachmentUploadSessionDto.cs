using System;

namespace Aig.Lms.Modules.Tickets.Application.Tickets.Dtos;

public sealed record AttachmentUploadSessionDto(
    Guid AttachmentId,
    string FileName,
    string UploadUrl,
    string ObjectKey,
    DateTimeOffset ExpiresAtUtc);
