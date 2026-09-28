namespace PaddyWise.Api.Services.Shared;

public interface IEmailService
{
    Task SendOfficerApprovalEmailAsync(string toEmail, string officerName);
}
