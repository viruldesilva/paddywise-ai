using Resend;

namespace PaddyWise.Api.Services.Shared;

public class EmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IResend resend, IConfiguration config, ILogger<EmailService> logger)
    {
        _resend = resend;
        _config = config;
        _logger = logger;
    }

    public async Task SendOfficerApprovalEmailAsync(string toEmail, string officerName)
    {
        try
        {
            var fromEmail = _config["Resend:FromEmail"] ?? "onboarding@resend.dev";
            var fromName = _config["Resend:FromName"] ?? "PaddyWise AI";

            // Note: onboarding@resend.dev (the FromEmail) is Resend's shared test sender
            // and can currently only deliver to the email address the Resend account itself was
            // signed up with, until a custom domain is verified — this is expected for development/demo
            // purposes, not a bug.

            var message = new EmailMessage
            {
                From = $"{fromName} <{fromEmail}>",
                Subject = "Your PaddyWise Officer Account Has Been Approved",
                HtmlBody = $@"
                    <div style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
                        <h2 style=""color: #2e7d32;"">PaddyWise AI - Account Approved</h2>
                        <p>Hello {officerName},</p>
                        <p>Your Agricultural Officer account application has been reviewed and <strong>approved</strong> by the administrator.</p>
                        <p>You can now sign in to the PaddyWise Officer Portal to access field management, cultivation cycle approvals, pest surveillance, and advisory services.</p>
                        <p style=""margin-top: 25px;"">Best regards,<br/>The PaddyWise AI Team</p>
                    </div>"
            };
            message.To.Add(toEmail);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await _resend.EmailSendAsync(message, cts.Token);
            _logger.LogInformation("Officer approval email sent successfully to {Email} for {OfficerName}.", toEmail, officerName);
        }
        catch (Exception ex)
        {
            // On any failure, log it but do NOT throw — the approve endpoint's status update must already have
            // succeeded and must not be rolled back just because the email failed.
            _logger.LogError(ex, "Failed to send officer approval email to {Email} for {OfficerName}.", toEmail, officerName);
        }
    }
}
