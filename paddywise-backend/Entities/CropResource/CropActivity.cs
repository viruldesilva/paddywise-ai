using System;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Entities.CropResource;

public class CropActivity
{
    public int Id { get; set; }

    public int CultivationCycleId { get; set; }
    public CultivationCycle? CultivationCycle { get; set; }

    public CropActivityType ActivityType { get; set; }
    public DateOnly Date { get; set; }

    /// <summary>
    /// JSONB column containing the specific data for this activity type
    /// e.g. fertilizer quantity, irrigation water level, etc.
    /// </summary>
    public string DetailsJson { get; set; } = "{}";

    public int LoggedByUserId { get; set; }
    public User? LoggedByUser { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
