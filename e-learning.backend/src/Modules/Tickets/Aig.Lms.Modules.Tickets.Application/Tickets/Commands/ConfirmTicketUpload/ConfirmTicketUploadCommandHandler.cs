using Aig.Lms.BuildingBlocks.Application.Abstractions;
using Aig.Lms.Modules.Tickets.Domain.Entities;
using Aig.Lms.Modules.Tickets.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Aig.Lms.Modules.Tickets.Application.Tickets.Commands.ConfirmTicketUpload;

public sealed class ConfirmTicketUploadCommandHandler
{
    private readonly ITicketRepository _repository;
    private readonly IObjectStorage _objectStorage;
    private readonly IConfiguration _configuration;

    public ConfirmTicketUploadCommandHandler(
        ITicketRepository repository,
        IObjectStorage objectStorage,
        IConfiguration configuration)
    {
        _repository = repository;
        _objectStorage = objectStorage;
        _configuration = configuration;
    }

    public async Task HandleAsync(ConfirmTicketUploadCommand command, CancellationToken ct = default)
    {
        // 1. Lấy thông tin Ticket từ DB
        var ticket = await _repository.GetByIdAsync(command.TicketId, ct);
        if (ticket is null)
            throw new InvalidOperationException("Ticket không tồn tại.");

        if (ticket.TenantId != command.TenantId)
            throw new InvalidOperationException("Bạn không có quyền truy cập Ticket này.");

        var bucket = _configuration["ObjectStorage:ContentBucket"] ?? "lms-content";

        // 2. Lấy toàn bộ metadata từ MinIO TRƯỚC để đảm bảo toàn bộ file đều đã được upload
        var validMetadata = new System.Collections.Generic.List<(TicketAttachment Attachment, StoredObjectMetadata Metadata)>();

        foreach (var attachment in ticket.Attachments)
        {
            // Bỏ qua các file đã được xác nhận (nếu có logic xác nhận nhiều lần)
            if (attachment.FileSize > 0) continue;

            try
            {
                var metadata = await _objectStorage.GetObjectMetadataAsync(bucket, attachment.FilePath, ct);
                validMetadata.Add((attachment, metadata));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Không thể xác thực file {attachment.FileName} trên Storage. Lỗi: {ex.Message}", ex);
            }
        }

        // 3. Cập nhật lại Database sau khi chắc chắn 100% file đã hợp lệ
        foreach (var item in validMetadata)
        {
            item.Attachment.ConfirmUpload(
                item.Metadata.ContentType ?? "application/octet-stream",
                item.Metadata.SizeBytes);

            // Lưu thay đổi của File đính kèm vào Database
            await _repository.UpdateAttachmentAsync(item.Attachment, ct);
        }
    }
}
