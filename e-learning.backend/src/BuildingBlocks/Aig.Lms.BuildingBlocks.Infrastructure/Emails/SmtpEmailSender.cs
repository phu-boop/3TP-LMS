using Aig.Lms.BuildingBlocks.Application.Emails;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace Aig.Lms.BuildingBlocks.Infrastructure.Emails;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEmailTokenProvider? _tokenProvider;

    public SmtpEmailSender(IOptions<SmtpSettings> options, IConfiguration configuration, ILogger<SmtpEmailSender> logger, IEmailTokenProvider? tokenProvider = null)
    {
        _settings = options.Value;
        _configuration = configuration;
        _logger = logger;
        _tokenProvider = tokenProvider;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

            // Resolve access token (env var -> config -> token provider)
            var accessToken = Environment.GetEnvironmentVariable("SMTP_ACCESS_TOKEN")
                              ?? _configuration[$"{SmtpSettings.SectionName}:AccessToken"]
                              ?? _settings.AccessToken;

            if (string.IsNullOrWhiteSpace(accessToken) && _tokenProvider != null)
            {
                accessToken = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogError("SMTP OAuth2 access token is missing. Check ClientId, ClientSecret, RefreshToken configuration.");
                throw new InvalidOperationException("SMTP OAuth2 access token is missing.");
            }

            using var smtp = new SmtpClient();
            var secureSocketOptions = _settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;

            await smtp.ConnectAsync(_settings.Host, _settings.Port, secureSocketOptions, cancellationToken);

            var oauth2 = new MailKit.Security.SaslMechanismOAuth2(_settings.FromEmail, accessToken);
            await smtp.AuthenticateAsync(oauth2, cancellationToken);

            await smtp.SendAsync(email, cancellationToken);
            await smtp.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
            throw;
        }
    }
}
