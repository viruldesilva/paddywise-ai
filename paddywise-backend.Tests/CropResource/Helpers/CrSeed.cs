using System.Text.Json;
using PaddyWise.Api.Data;
using PaddyWise.Api.Entities.CropResource;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Backend.Tests.FieldCultivation.Helpers;

namespace PaddyWise.Backend.Tests.CropResource.Helpers;

/// <summary>
/// Component 2 seeding on top of FieldCultivation's FcTestDb (division, variety, farmer,
/// field and cycle). Activity details are built with exactly the keys
/// CropActivityService.ValidateActivityDetails and CropActivityTools read.
/// </summary>
public static class CrSeed
{
    public static DateOnly Today => FcTestDb.Today;

    public static string Fertilizer(string type = "Urea", object? quantity = null, string cropStage = "Tillering",
        string region = "Dry", string method = "Broadcasting") =>
        JsonSerializer.Serialize(new { type, quantity = quantity ?? 50, cropStage, region, method });

    public static string Irrigation(object? waterLevel = null, object? duration = null, string source = "Canal") =>
        JsonSerializer.Serialize(new { waterLevel = waterLevel ?? 3, duration = duration ?? 4, source });

    public static string Pesticide(string product = "Fipronil 50 SC", string targetPest = "Stem borer",
        object? quantity = null, string method = "Spraying") =>
        JsonSerializer.Serialize(new { product, targetPest, quantity = quantity ?? 1.5, method });

    public static string Other(string specificActivity = "Weeding", string notes = "") =>
        JsonSerializer.Serialize(new { specificActivity, notes });

    public static async Task<CropActivity> AddActivityAsync(
        ApplicationDbContext db,
        CultivationCycle cycle,
        int loggedByUserId,
        CropActivityType type,
        DateOnly date,
        string detailsJson,
        DateTimeOffset? createdAt = null)
    {
        var activity = new CropActivity
        {
            CultivationCycleId = cycle.Id,
            ActivityType = type,
            Date = date,
            DetailsJson = detailsJson,
            LoggedByUserId = loggedByUserId,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow
        };
        db.CropActivities.Add(activity);
        await db.SaveChangesAsync();
        return activity;
    }

    public static async Task<CropActivityRecommendation> AddRecommendationAsync(
        ApplicationDbContext db,
        CultivationCycle cycle,
        int requestedByUserId,
        string status = "PENDING_OFFICER_REVIEW",
        string action = "Irrigate to establish a 2 - 4 cm standing water depth.",
        string? executionPayloadJson = null,
        DateTime? createdAt = null)
    {
        var rec = new CropActivityRecommendation
        {
            CultivationCycleId = cycle.Id,
            RequestedByUserId = requestedByUserId,
            Category = "Irrigation",
            Priority = "HIGH",
            Action = action,
            Reason = "Test.",
            Evidence = "Test.",
            Status = status,
            ExecutionPayloadJson = executionPayloadJson ?? JsonSerializer.Serialize(new
            {
                activityType = "Irrigation",
                detailsJson = Irrigation(3, 2, "Canal")
            }),
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
        db.CropActivityRecommendations.Add(rec);
        await db.SaveChangesAsync();
        return rec;
    }
}
