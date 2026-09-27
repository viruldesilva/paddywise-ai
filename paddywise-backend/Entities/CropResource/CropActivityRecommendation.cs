using System;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Entities.CropResource;

/// <summary>
/// A persistent agronomic recommendation produced by the Crop Activity Agentic AI.
/// Tracks lifecycle: PENDING_OFFICER_REVIEW -> APPROVED / REJECTED -> EXECUTED by farmer.
/// </summary>
public class CropActivityRecommendation
{
    public int Id { get; set; }

    public string RecommendationUid { get; set; } = Guid.NewGuid().ToString("N");

    public int CultivationCycleId { get; set; }
    public CultivationCycle? CultivationCycle { get; set; }

    public int RequestedByUserId { get; set; }
    public User? RequestedByUser { get; set; }

    public string Category { get; set; } = "General"; // Irrigation, Fertilizer, Pest, General
    public string Priority { get; set; } = "MEDIUM"; // HIGH, MEDIUM, LOW
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; } = 0.85;
    public string CitationsJson { get; set; } = "[]";
    public bool RequiresOfficerReview { get; set; } = true;

    /// <summary>
    /// Lifecycle Status:
    /// PENDING_OFFICER_REVIEW - Sent to Agricultural Officer for approval
    /// APPROVED               - Verified & approved by Agricultural Officer, farmer can now execute
    /// REJECTED               - Rejected by Agricultural Officer with guidance comment
    /// EXECUTED               - Action committed by farmer into the field activity ledger
    /// </summary>
    public string Status { get; set; } = "PENDING_OFFICER_REVIEW";

    /// <summary>
    /// JSON payload with pre-configured activity parameters ready for one-click execution.
    /// </summary>
    public string? ExecutionPayloadJson { get; set; }

    // Agricultural Officer Review Trail
    public int? OfficerId { get; set; }
    public User? Officer { get; set; }
    public string? OfficerName { get; set; }
    public string? OfficerComment { get; set; }
    public DateTime? ReviewedAt { get; set; }

    // Farmer Execution Trail
    public int? ExecutedByUserId { get; set; }
    public User? ExecutedByUser { get; set; }
    public int? ExecutedActivityId { get; set; }
    public CropActivity? ExecutedActivity { get; set; }
    public DateTime? ExecutedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
