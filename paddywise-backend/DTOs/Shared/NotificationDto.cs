using System;

namespace PaddyWise.Api.DTOs.Shared;

public class NotificationDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "CropActivityReview";
    public string Status { get; set; } = string.Empty;
    public int? RelatedCycleId { get; set; }
    public int? RelatedRecommendationId { get; set; }
    public string? OfficerName { get; set; }
    public string? OfficerComment { get; set; }
    public string? ActionText { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
