namespace Aig.Lms.BuildingBlocks.Application.Emails;

public interface IEmailTokenProvider
{
    /// <summary>
    /// Returns an OAuth2 access token for SMTP (XOAUTH2) given SmtpSettings context.
    /// Implementations should cache tokens as appropriate.
    /// </summary>
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
