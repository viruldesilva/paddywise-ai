using PaddyWise.Api.DTOs.ReportingApproval;

namespace PaddyWise.Api.Services.ReportingApproval;

public interface IOfficerDashboardService
{
    Task<OfficerDashboardDto?> GetOfficerDashboardAsync(int officerId, CancellationToken ct = default);
}
