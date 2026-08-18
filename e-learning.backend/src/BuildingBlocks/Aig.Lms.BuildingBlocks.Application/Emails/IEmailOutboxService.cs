namespace Aig.Lms.BuildingBlocks.Application.Emails;

/// <summary>
/// Simple facade — các module chỉ cần inject interface này và gọi EnqueueAsync() là xong.
/// Worker sẽ tự bốc email đi gửi sau mỗi 10 giây.
/// </summary>
public interface IEmailOutboxService
{
    /// <summary>
    /// Đẩy 1 email vào hàng đợi (Outbox).
    /// - templateName: tên file .cshtml trong thư mục Templates/ (VD: "TicketResolved")
    /// - payload: object chứa data để render template (VD: new { UserName = "...", TicketCode = "..." })
    /// </summary>
    Task EnqueueAsync(string toEmail, string subject, string templateName, object payload, CancellationToken cancellationToken = default);
}
