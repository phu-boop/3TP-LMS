using Aig.Lms.BuildingBlocks.Application.Emails;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Aig.Lms.UnitTests.Emails;

/// <summary>
/// File này đóng vai trò HƯỚNG DẪN MẪU (Sample Reference) cho các Developer khi viết Unit Test
/// cho tính năng nghiệp vụ của mình có gọi đến dịch vụ gửi Mail (IEmailOutboxService).
/// </summary>
public class SampleEmailFeatureCommandHandlerTests
{
    private readonly IEmailOutboxService _emailOutboxService = Substitute.For<IEmailOutboxService>();
    private readonly DummyResetPasswordService _handler;

    public SampleEmailFeatureCommandHandlerTests()
    {
        _handler = new DummyResetPasswordService(_emailOutboxService);
    }

    [Fact]
    public async Task RequestPasswordReset_ValidEmail_EnqueuesEmailOutboxWithCorrectPayload()
    {
        // Arrange
        var userEmail = "dev.test@aig.edu.vn";
        var resetToken = "ABC-123-XYZ";

        // Act
        await _handler.RequestPasswordResetAsync(userEmail, resetToken, CancellationToken.None);

        // Assert
        await _emailOutboxService.Received(1).EnqueueAsync(
            toEmail: userEmail,
            subject: Arg.Is<string>(s => s.Contains("Đặt lại mật khẩu")),
            templateName: "ResetPasswordToken",
            payload: Arg.Is<object>(p => p != null),
            cancellationToken: Arg.Any<CancellationToken>()
        );
    }
}

/// <summary>
/// Lớp Dummy đại diện cho Command Handler / Service của Developer trong các Module nghiệp vụ (VD: Identity, Users, Support).
/// </summary>
public class DummyResetPasswordService
{
    private readonly IEmailOutboxService _emailOutboxService;

    public DummyResetPasswordService(IEmailOutboxService emailOutboxService)
    {
        _emailOutboxService = emailOutboxService;
    }

    public async Task RequestPasswordResetAsync(string email, string token, CancellationToken ct)
    {
        // 1. Logic tạo token reset mật khẩu...

        // 2. Gọi email framework để đưa email vào Outbox queue
        await _emailOutboxService.EnqueueAsync(
            toEmail: email,
            subject: "[AIG LMS] Yêu cầu Đặt lại mật khẩu tài khoản",
            templateName: "ResetPasswordToken",
            payload: new
            {
                Email = email,
                ResetToken = token,
                ExpireTimeInMinutes = 15
            },
            cancellationToken: ct
        );
    }
}
