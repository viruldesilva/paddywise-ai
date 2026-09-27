using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Entities.ReportingApproval;

/// <summary>
/// In-app notification for users (e.g. farmers receiving officer decision updates).
/// </summary>
public class Notification
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
