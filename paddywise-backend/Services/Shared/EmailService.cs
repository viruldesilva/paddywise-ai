using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PaddyWise.Api.Services.Shared;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;
    private readonly IHttpClientFactory? _httpClientFactory;

    public EmailService(
        IConfiguration config,
        ILogger<EmailService> logger,
        IHttpClientFactory? httpClientFactory = null)
    {
        _config = config;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task SendOfficerApprovalEmailAsync(string toEmail, string officerName)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var htmlBody = GetOfficerApprovalHtmlBody(officerName);

            var brevoApiKey = _config["Brevo:ApiKey"];
            var smtpHost = _config["Smtp:Host"];
            var smtpPassword = _config["Smtp:Password"];

            // 1. Send via Brevo REST API if API key is configured (works over HTTPS port 443 to ALL emails)
            if (!string.IsNullOrWhiteSpace(brevoApiKey))
            {
                var fromEmail = _config["Brevo:FromEmail"] ?? "no-reply@paddywise.ai";
                var fromName = _config["Brevo:FromName"] ?? "PaddyWise AI";
                await SendViaBrevoApiAsync(brevoApiKey, fromEmail, fromName, toEmail, officerName, htmlBody, cts.Token);
                return;
            }

            // 2. Send via SMTP Relay if Host and Password are provided (e.g. Brevo SMTP relay)
            if (!string.IsNullOrWhiteSpace(smtpHost) && !string.IsNullOrWhiteSpace(smtpPassword))
            {
                var port = int.TryParse(_config["Smtp:Port"], out var parsedPort) ? parsedPort : 587;
                var enableSsl = !bool.TryParse(_config["Smtp:EnableSsl"], out var parsedSsl) || parsedSsl;
                var username = _config["Smtp:UserName"] ?? string.Empty;
                var fromEmail = _config["Smtp:FromEmail"] ?? username;
                var fromName = _config["Smtp:FromName"] ?? "PaddyWise AI";
                await SendViaSmtpAsync(smtpHost, port, enableSsl, username, smtpPassword, fromEmail, fromName, toEmail, officerName, htmlBody, cts.Token);
                return;
            }

            _logger.LogWarning("No email provider configured. Please provide Brevo:ApiKey or Smtp:Host/Password in appsettings.");
        }
        catch (Exception ex)
        {
            // On any failure, log it but do NOT throw — the approve endpoint's status update must already have
            // succeeded and must not be rolled back just because the email failed.
            _logger.LogError(ex, "Failed to send officer approval email to {Email} for {OfficerName}.", toEmail, officerName);
        }
    }

    private async Task SendViaBrevoApiAsync(
        string apiKey,
        string fromEmail,
        string fromName,
        string toEmail,
        string officerName,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory?.CreateClient("BrevoClient") ?? new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        request.Headers.Add("api-key", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var payload = new
        {
            sender = new { name = fromName, email = fromEmail },
            to = new[] { new { email = toEmail, name = officerName } },
            subject = "Your PaddyWise Officer Account Has Been Approved",
            htmlContent = htmlBody
        };

        request.Content = JsonContent.Create(payload);
        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Brevo API returned status {(int)response.StatusCode} ({response.ReasonPhrase}): {errorBody}");
        }

        _logger.LogInformation("Officer approval email sent successfully via Brevo to {Email} for {OfficerName}.", toEmail, officerName);
    }

    private async Task SendViaSmtpAsync(
        string host,
        int port,
        bool enableSsl,
        string username,
        string password,
        string fromEmail,
        string fromName,
        string toEmail,
        string officerName,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        using var client = new System.Net.Mail.SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            UseDefaultCredentials = false,
            Credentials = new System.Net.NetworkCredential(username, password),
            DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network
        };

        using var message = new System.Net.Mail.MailMessage
        {
            From = new System.Net.Mail.MailAddress(fromEmail, fromName),
            Subject = "Your PaddyWise Officer Account Has Been Approved",
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(new System.Net.Mail.MailAddress(toEmail, officerName));

        await client.SendMailAsync(message, cancellationToken);
        _logger.LogInformation("Officer approval email sent successfully via SMTP ({Host}:{Port}) to {Email} for {OfficerName}.", host, port, toEmail, officerName);
    }

    private static string GetOfficerApprovalHtmlBody(string officerName) =>
        $@"
        <div style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
            <h2 style=""color: #2e7d32;"">PaddyWise AI - Account Approved</h2>
            <p>Hello {officerName},</p>
            <p>Your Agricultural Officer account application has been reviewed and <strong>approved</strong> by the administrator.</p>
            <p>You can now sign in to the PaddyWise Officer Portal to access field management, cultivation cycle approvals, pest surveillance, and advisory services.</p>
            <p style=""margin-top: 25px;"">Best regards,<br/>The PaddyWise AI Team</p>
        </div>";
}
