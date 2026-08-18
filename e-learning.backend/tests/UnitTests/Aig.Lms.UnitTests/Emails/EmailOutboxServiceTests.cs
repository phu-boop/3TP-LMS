using System.Text.Json;
using Aig.Lms.BuildingBlocks.Application.Emails;
using Aig.Lms.BuildingBlocks.Domain.Emails;
using Aig.Lms.BuildingBlocks.Infrastructure.Emails;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Aig.Lms.UnitTests.Emails;

public class EmailOutboxServiceTests
{
    private readonly IEmailOutboxRepository _repository = Substitute.For<IEmailOutboxRepository>();
    private readonly EmailOutboxService _service;

    public EmailOutboxServiceTests()
    {
        _service = new EmailOutboxService(_repository);
    }

    [Fact]
    public async Task EnqueueAsync_ValidPayload_SerializesPayloadAndInsertsMessage()
    {
        // Arrange
        var toEmail = "user@aig.edu.vn";
        var subject = "Chào mừng bạn gia nhập AIG LMS!";
        var templateName = "WelcomeUser";
        var payload = new
        {
            FullName = "Nguyen Van A",
            Username = "nguyenvana"
        };
        var expectedJson = JsonSerializer.Serialize(payload);

        // Act
        await _service.EnqueueAsync(toEmail, subject, templateName, payload, CancellationToken.None);

        // Assert
        await _repository.Received(1).InsertAsync(
            Arg.Is<EmailOutboxMessage>(msg =>
                msg.ToEmail == toEmail &&
                msg.Subject == subject &&
                msg.TemplateName == templateName &&
                msg.PayloadJson == expectedJson &&
                msg.Status == EmailOutboxMessageStatus.Pending),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnqueueAsync_NullPayload_SerializesNullAndInsertsMessage()
    {
        // Arrange
        var toEmail = "user@aig.edu.vn";
        var subject = "Thông báo hệ thống";
        var templateName = "SystemNotice";
        object? payload = null;

        // Act
        await _service.EnqueueAsync(toEmail, subject, templateName, payload!, CancellationToken.None);

        // Assert
        await _repository.Received(1).InsertAsync(
            Arg.Is<EmailOutboxMessage>(msg =>
                msg.ToEmail == toEmail &&
                msg.Subject == subject &&
                msg.TemplateName == templateName &&
                msg.PayloadJson == "null" &&
                msg.Status == EmailOutboxMessageStatus.Pending),
            Arg.Any<CancellationToken>());
    }
}
