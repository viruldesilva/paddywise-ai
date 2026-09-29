using System;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Entities.Shared;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    
    // Type: "CropActivityReview", "CropActivityExecution", "Alert", "General"
    public string Type { get; set; } = "CropActivityReview";

    // Status: "APPROVED", "REJECTED", "PENDING", "EXECUTED"
    public string Status { get; set; } = string.Empty;

    public int? RelatedCycleId { get; set; }
    public int? RelatedRecommendationId { get; set; }
    
    public string? OfficerName { get; set; }
    public string? OfficerComment { get; set; }
    public string? ActionText { get; set; }

    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
