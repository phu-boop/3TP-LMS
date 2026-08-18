namespace Aig.Lms.BuildingBlocks.Infrastructure.Emails;

public class SmtpSettings
{
    public const string SectionName = "SmtpSettings";
    
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string FromName { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = false;

    // OAuth2 Credentials & Tokens
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string TokenEndpoint { get; set; } = string.Empty;
    public string TokenGrantType { get; set; } = string.Empty;

    // Polling interval for background processor job (in seconds)
    public int PollingIntervalSeconds { get; set; } = 10;
}
