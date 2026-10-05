using PaddyWise.Api.Data;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.PestDisease;
using PaddyWise.Api.Entities.Shared;
using PaddyWise.Backend.Tests.FieldCultivation.Helpers;

namespace PaddyWise.Backend.Tests.ReportingApproval.Helpers;

/// <summary>
/// Component 4 seeding on top of FieldCultivation's FcTestDb: plans with a given PlanJson,
/// pest reports with their observation, and notifications.
/// </summary>
public static class RaSeed
{
    public static async Task<CultivationPlan> PlanAsync(
        ApplicationDbContext db,
        CultivationCycle cycle,
        int farmerId,
        string planJson,
        PlanStatus status = PlanStatus.PendingOfficerApproval,
        string objective = "Good yield this Maha.",
        string? validationErrorsJson = null,
        string? officerComment = null)
    {
        var plan = new CultivationPlan
        {
            CultivationCycleId = cycle.Id,
            RequestedByUserId = farmerId,
            Objective = objective,
            PlanJson = planJson,
            Status = status,
            ValidationErrorsJson = validationErrorsJson,
            OfficerComment = officerComment
        };
        db.CultivationPlans.Add(plan);
        await db.SaveChangesAsync();
        return plan;
    }

    public static async Task<PestDiseaseReport> ReportAsync(
        ApplicationDbContext db,
        CultivationCycle cycle,
        int reportedByUserId,
        string possibleIssue = "Rice Blast",
        decimal confidence = 0.8m,
        string symptoms = "Diamond-shaped grey lesions on the leaves.",
        PestDiseaseReportStatus status = PestDiseaseReportStatus.PendingOfficerReview,
        string? officerComment = null)
    {
        var observation = new CropObservation
        {
            CultivationCycleId = cycle.Id,
            ReportedByUserId = reportedByUserId,
            ObservationType = ObservationType.Disease,
            CropStage = cycle.CurrentStage,
            Symptoms = symptoms,
            Severity = ObservationSeverity.Moderate
        };
        db.CropObservations.Add(observation);
        await db.SaveChangesAsync();

        var report = new PestDiseaseReport
        {
            CropObservationId = observation.Id,
            PossibleIssue = possibleIssue,
            Confidence = confidence,
            Status = status,
            OfficerComment = officerComment
        };
        db.PestDiseaseReports.Add(report);
        await db.SaveChangesAsync();
        return report;
    }

    public static Notification Notification(int userId, string title, DateTime createdAt,
        bool isRead = false, string type = "CultivationPlanReview") => new()
    {
        UserId = userId,
        Title = title,
        Message = title + " message",
        Type = type,
        Status = "APPROVED",
        IsRead = isRead,
        CreatedAt = createdAt
    };
}
