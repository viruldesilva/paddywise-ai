namespace PaddyWise.Api.DTOs.Shared;

public class AdminDashboardDto
{
    public int TotalUsers { get; set; }
    public Dictionary<string, int> UsersPerRole { get; set; } = new();
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int PendingOfficerApprovals { get; set; }
}
