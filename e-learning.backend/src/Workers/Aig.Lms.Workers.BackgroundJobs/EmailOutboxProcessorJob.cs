using Aig.Lms.BuildingBlocks.Application.Emails;
using Aig.Lms.BuildingBlocks.Domain.Emails;
using Aig.Lms.BuildingBlocks.Infrastructure.Emails;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aig.Lms.Workers.BackgroundJobs;

public class EmailOutboxProcessorJob : BackgroundService
{
    private readonly IEmailOutboxRepository _outboxRepository;
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateEngine _templateEngine;
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger<EmailOutboxProcessorJob> _logger;

    public EmailOutboxProcessorJob(
        IEmailOutboxRepository outboxRepository,
        IEmailSender emailSender,
        IEmailTemplateEngine templateEngine,
        IOptions<SmtpSettings> smtpSettingsOptions,
        ILogger<EmailOutboxProcessorJob> logger)
    {
        _outboxRepository = outboxRepository;
        _emailSender = emailSender;
        _templateEngine = templateEngine;
        _smtpSettings = smtpSettingsOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailOutboxProcessorJob is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing email outbox.");
            }

            var interval = _smtpSettings.PollingIntervalSeconds > 0 ? _smtpSettings.PollingIntervalSeconds : 10;
            await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken);
        }
    }

    internal async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        var messages = await _outboxRepository.GetPendingMessagesAsync(50, cancellationToken);
        
        if (messages.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Found {Count} pending emails to send.", messages.Count);

        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                await _outboxRepository.UpdateStatusAsync(message.Id, EmailOutboxMessageStatus.Processing, null, null, cancellationToken);

                // Render HTML template
                var htmlBody = await _templateEngine.RenderAsync(message.TemplateName, message.PayloadJson);

                // Send Email via SMTP
                await _emailSender.SendAsync(message.ToEmail, message.Subject, htmlBody, cancellationToken);

                // Mark as Sent
                await _outboxRepository.UpdateStatusAsync(message.Id, EmailOutboxMessageStatus.Sent, DateTime.UtcNow, null, cancellationToken);
                
                _logger.LogInformation("Successfully sent email ID: {Id}", message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email ID: {Id}", message.Id);
                
                // Mark as Failed
                await _outboxRepository.UpdateStatusAsync(message.Id, EmailOutboxMessageStatus.Failed, DateTime.UtcNow, ex.Message, cancellationToken);
            }
        }
    }
}
