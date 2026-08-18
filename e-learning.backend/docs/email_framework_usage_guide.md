# Hướng Dẫn Sử Dụng Email Framework (AIG LMS)

Tài liệu hướng dẫn dành cho Backend Developer để gửi email trong dự án LMS.

Hệ thống sử dụng **Outbox Pattern**: Khi gọi gửi email, dữ liệu sẽ lưu vào Database (`email_outbox_message`). Một **Background Worker** chạy ngầm định kỳ (cấu hình trong `appsettings`) sẽ tự động bốc mail đi gửi qua **Gmail OAuth2**.

---

## 🛠 2 Bước Để Gửi Email Trong Module

### Bước 1: Tạo Template Razor (`.cshtml`)
- **Vị trí**: `src/Workers/Aig.Lms.Workers.BackgroundJobs/Templates/`
- **Tên file**: Đặt theo tên tính năng, ví dụ `WelcomeUser.cshtml`.

**Ví dụ nội dung `WelcomeUser.cshtml`**:
```html
@model dynamic
<!DOCTYPE html>
<html>
<body>
    <h2>Chào mừng @Model.FullName đến với AIG LMS!</h2>
    <p>Tên đăng nhập của bạn là: <strong>@Model.Username</strong></p>
</body>
</html>
```

---

### Bước 2: Gọi gửi email trong Service / Command Handler
Inject `IEmailOutboxService` và gọi `EnqueueAsync()`.

```csharp
using Aig.Lms.BuildingBlocks.Application.Emails;

public class RegisterUserCommandHandler
{
    private readonly IEmailOutboxService _emailOutboxService;

    public RegisterUserCommandHandler(IEmailOutboxService emailOutboxService)
    {
        _emailOutboxService = emailOutboxService;
    }

    public async Task Handle(RegisterUserCommand request, CancellationToken ct)
    {
        // 1. Xử lý logic nghiệp vụ chính (tạo User, lưu DB...)
        // ...

        // 2. Đẩy email vào Outbox Queue
        await _emailOutboxService.EnqueueAsync(
            toEmail: request.Email,
            subject: "Chào mừng bạn gia nhập AIG LMS!",
            templateName: "WelcomeUser", // 👈 Tên file .cshtml (KHÔNG ghi đuôi .cshtml)
            payload: new                 // 👈 Dữ liệu động truyền vào @Model trong Razor
            {
                FullName = request.FullName,
                Username = request.Username
            },
            cancellationToken: ct
        );
    }
}
```

---

## 💡 Quy Tắc Cần Chú Ý
1. **Tên Template (`templateName`)**: Khớp chính xác với tên file trong thư mục `Templates/` (phân biệt hoa thường, không ghi `.cshtml`).
2. **Dữ liệu động (`payload`)**: Truyền dưới dạng **Anonymous Object** (`new { Key = Value }`). Các key sẽ được gọi bằng `@Model.Key` trong file `.cshtml`.
3. **Không gọi trực tiếp SMTP/MailKit**: Mọi tính năng gửi mail BẮT BUỘC dùng `IEmailOutboxService.EnqueueAsync()` để đảm bảo không bị block API và không mất dữ liệu khi lỗi mạng.

---

## 🧪 Viết Unit Test (Sử dụng NSubstitute)

```csharp
[Fact]
public async Task Handle_ValidRequest_EnqueuesEmail()
{
    // Arrange
    var emailService = Substitute.For<IEmailOutboxService>();
    var handler = new RegisterUserCommandHandler(emailService);

    // Act
    await handler.Handle(command, CancellationToken.None);

    // Assert
    await emailService.Received(1).EnqueueAsync(
        toEmail: "test@example.com",
        subject: Arg.Any<string>(),
        templateName: "WelcomeUser",
        payload: Arg.Is<object>(p => p != null),
        cancellationToken: Arg.Any<CancellationToken>()
    );
}
```
