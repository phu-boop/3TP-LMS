using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Aig.Lms.BuildingBlocks.Application.Emails;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aig.Lms.BuildingBlocks.Infrastructure.Emails;

public class OAuth2EmailTokenProvider : IEmailTokenProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SmtpSettings _settings;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OAuth2EmailTokenProvider> _logger;

    public OAuth2EmailTokenProvider(IHttpClientFactory httpClientFactory, IOptions<SmtpSettings> options, IConfiguration configuration, ILogger<OAuth2EmailTokenProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = options.Value;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // Priority: env var SMTP_ACCESS_TOKEN -> config SmtpSettings.AccessToken -> try token endpoint
        var accessToken = Environment.GetEnvironmentVariable("SMTP_ACCESS_TOKEN")
                          ?? _configuration[$"{SmtpSettings.SectionName}:AccessToken"]
                          ?? _settings.AccessToken;

        if (!string.IsNullOrEmpty(accessToken)) return accessToken;

        var tokenEndpoint = Environment.GetEnvironmentVariable("SMTP_TOKEN_ENDPOINT")
                            ?? _configuration[$"{SmtpSettings.SectionName}:TokenEndpoint"]
                            ?? _settings.TokenEndpoint;

        if (string.IsNullOrEmpty(tokenEndpoint))
        {
            _logger.LogWarning("No token endpoint configured for SMTP OAuth2.");
            return null;
        }

        var clientId = Environment.GetEnvironmentVariable("SMTP_CLIENT_ID")
                       ?? _configuration[$"{SmtpSettings.SectionName}:ClientId"]
                       ?? _settings.ClientId;

        var clientSecret = Environment.GetEnvironmentVariable("SMTP_CLIENT_SECRET")
                           ?? _configuration[$"{SmtpSettings.SectionName}:ClientSecret"]
                           ?? _settings.ClientSecret;

        var grantType = Environment.GetEnvironmentVariable("SMTP_TOKEN_GRANT_TYPE")
                        ?? _configuration[$"{SmtpSettings.SectionName}:TokenGrantType"]
                        ?? _settings.TokenGrantType;

        var refreshToken = Environment.GetEnvironmentVariable("SMTP_REFRESH_TOKEN")
                           ?? _configuration[$"{SmtpSettings.SectionName}:RefreshToken"]
                           ?? _settings.RefreshToken;

        var client = _httpClientFactory.CreateClient();

        var form = new List<KeyValuePair<string, string>>();

        if (!string.IsNullOrEmpty(grantType) && grantType.Equals("refresh_token", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogError("TokenGrantType=refresh_token but no refresh token configured");
                return null;
            }

            form.Add(new KeyValuePair<string, string>("grant_type", "refresh_token"));
            form.Add(new KeyValuePair<string, string>("refresh_token", refreshToken));
            if (!string.IsNullOrEmpty(clientId)) form.Add(new KeyValuePair<string, string>("client_id", clientId));
            if (!string.IsNullOrEmpty(clientSecret)) form.Add(new KeyValuePair<string, string>("client_secret", clientSecret));
        }
        else
        {
            // default to client_credentials
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogError("Client credentials missing for client_credentials grant");
                return null;
            }

            form.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
            form.Add(new KeyValuePair<string, string>("client_id", clientId));
            form.Add(new KeyValuePair<string, string>("client_secret", clientSecret));
        }

        var req = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(form)
        };

        try
        {
            var resp = await client.SendAsync(req, cancellationToken);
            if (!resp.IsSuccessStatusCode)
            {
                var errorBody = await resp.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to acquire SMTP OAuth2 token from {Endpoint}. Status: {Status}, Response: {Body}", tokenEndpoint, resp.StatusCode, errorBody);
                return null;
            }

            var json = await resp.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("access_token", out var at))
            {
                var token = at.GetString();
                _logger.LogDebug("Acquired SMTP OAuth2 access token via {Endpoint}", tokenEndpoint);
                return token;
            }

            _logger.LogError("Token response did not contain access_token: {Response}", json);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire SMTP OAuth2 token from {Endpoint}", tokenEndpoint);
            return null;
        }
    }
}
