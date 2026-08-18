using Aig.Lms.BuildingBlocks.Application.Emails;
using Aig.Lms.BuildingBlocks.Domain.Emails;
using Aig.Lms.Workers.BackgroundJobs;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Aig.Lms.UnitTests.Emails;

public class EmailOutboxProcessorJobTests
{
    private readonly IEmailOutboxRepository _outboxRepository = Substitute.For<IEmailOutboxRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IEmailTemplateEngine _templateEngine = Substitute.For<IEmailTemplateEngine>();
    private readonly ILogger<EmailOutboxProcessorJob> _logger = Substitute.For<ILogger<EmailOutboxProcessorJob>>();
    private readonly EmailOutboxProcessorJob _job;

    public EmailOutboxProcessorJobTests()
    {
        _job = new EmailOutboxProcessorJob(_outboxRepository, _emailSender, _templateEngine, _logger);
    }

    [Fact]
    public async Task ProcessOutboxMessagesAsync_PendingMessagesExist_RendersTemplateSendsEmailAndMarksAsSent()
    {
        // Arrange
        var message = new EmailOutboxMessage("student@aig.edu.vn", "Welcome", "WelcomeUser", "{\"FullName\":\"Student\"}");
        var messages = new List<EmailOutboxMessage> { message };
        var renderedHtml = "<html><body>Welcome Student</body></html>";

        _outboxRepository.GetPendingMessagesAsync(50, Arg.Any<CancellationToken>())
            .Returns(messages);
        _templateEngine.RenderAsync("WelcomeUser", "{\"FullName\":\"Student\"}")
            .Returns(renderedHtml);

        // Act
        await _job.ProcessOutboxMessagesAsync(CancellationToken.None);

        // Assert
        // 1. Marked as processing
        await _outboxRepository.Received(1).UpdateStatusAsync(
            message.Id,
            EmailOutboxMessageStatus.Processing,
            null,
            null,
            Arg.Any<CancellationToken>());

        // 2. Render template
        await _templateEngine.Received(1).RenderAsync("WelcomeUser", "{\"FullName\":\"Student\"}");

        // 3. Send email via SMTP sender
        await _emailSender.Received(1).SendAsync("student@aig.edu.vn", "Welcome", renderedHtml, Arg.Any<CancellationToken>());

        // 4. Marked as sent
        await _outboxRepository.Received(1).UpdateStatusAsync(
            message.Id,
            EmailOutboxMessageStatus.Sent,
            Arg.Any<DateTime>(),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessOutboxMessagesAsync_EmailSendingFails_MarksMessageAsFailedWithError()
    {
        // Arrange
        var message = new EmailOutboxMessage("student@aig.edu.vn", "Welcome", "WelcomeUser", "{}");
        var messages = new List<EmailOutboxMessage> { message };
        var exceptionMessage = "SMTP server unavailable";

        _outboxRepository.GetPendingMessagesAsync(50, Arg.Any<CancellationToken>())
            .Returns(messages);
        _templateEngine.RenderAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns("<html></html>");
        _emailSender.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException(exceptionMessage)));

        // Act
        await _job.ProcessOutboxMessagesAsync(CancellationToken.None);

        // Assert
        await _outboxRepository.Received(1).UpdateStatusAsync(
            message.Id,
            EmailOutboxMessageStatus.Failed,
            Arg.Any<DateTime>(),
            exceptionMessage,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessOutboxMessagesAsync_NoPendingMessages_DoesNotRenderOrSend()
    {
        // Arrange
        _outboxRepository.GetPendingMessagesAsync(50, Arg.Any<CancellationToken>())
            .Returns(new List<EmailOutboxMessage>());

        // Act
        await _job.ProcessOutboxMessagesAsync(CancellationToken.None);

        // Assert
        await _templateEngine.DidNotReceiveWithAnyArgs().RenderAsync(default!, default!);
        await _emailSender.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default!, default);
    }
}
