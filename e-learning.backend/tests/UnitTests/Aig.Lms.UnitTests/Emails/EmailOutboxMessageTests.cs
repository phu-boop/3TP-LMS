using Aig.Lms.BuildingBlocks.Domain.Emails;
using FluentAssertions;
using Xunit;

namespace Aig.Lms.UnitTests.Emails;

public class EmailOutboxMessageTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var toEmail = "test@example.com";
        var subject = "Test Subject";
        var templateName = "WelcomeUser";
        var payloadJson = "{\"FullName\":\"Nguyen Van A\"}";
        var beforeCreation = DateTime.UtcNow;

        // Act
        var message = new EmailOutboxMessage(toEmail, subject, templateName, payloadJson);

        // Assert
        message.Id.Should().NotBeEmpty();
        message.ToEmail.Should().Be(toEmail);
        message.Subject.Should().Be(subject);
        message.TemplateName.Should().Be(templateName);
        message.PayloadJson.Should().Be(payloadJson);
        message.Status.Should().Be(EmailOutboxMessageStatus.Pending);
        message.CreatedAt.Should().BeOnOrAfter(beforeCreation);
        message.ProcessedAt.Should().BeNull();
        message.Error.Should().BeNull();
    }

    [Fact]
    public void MarkAsProcessing_UpdatesStatusToProcessing()
    {
        // Arrange
        var message = new EmailOutboxMessage("test@example.com", "Subject", "Template", "{}");

        // Act
        message.MarkAsProcessing();

        // Assert
        message.Status.Should().Be(EmailOutboxMessageStatus.Processing);
    }

    [Fact]
    public void MarkAsSent_UpdatesStatusAndProcessedAt()
    {
        // Arrange
        var message = new EmailOutboxMessage("test@example.com", "Subject", "Template", "{}");
        var processedAt = DateTime.UtcNow;

        // Act
        message.MarkAsSent(processedAt);

        // Assert
        message.Status.Should().Be(EmailOutboxMessageStatus.Sent);
        message.ProcessedAt.Should().Be(processedAt);
        message.Error.Should().BeNull();
    }

    [Fact]
    public void MarkAsFailed_UpdatesStatusProcessedAtAndError()
    {
        // Arrange
        var message = new EmailOutboxMessage("test@example.com", "Subject", "Template", "{}");
        var processedAt = DateTime.UtcNow;
        var errorMessage = "SMTP connection timeout";

        // Act
        message.MarkAsFailed(processedAt, errorMessage);

        // Assert
        message.Status.Should().Be(EmailOutboxMessageStatus.Failed);
        message.ProcessedAt.Should().Be(processedAt);
        message.Error.Should().Be(errorMessage);
    }
}
