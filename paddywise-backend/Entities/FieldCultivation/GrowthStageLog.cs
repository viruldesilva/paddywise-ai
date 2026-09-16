using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Entities.FieldCultivation;

/// <summary>An observation that a cycle has reached a given growth stage.</summary>
public class GrowthStageLog
{
    public int Id { get; set; }

    public int CultivationCycleId { get; set; }

    public GrowthStage Stage { get; set; }
    public DateOnly ObservedOn { get; set; }
    public string? Notes { get; set; }

    // The farmer or officer who recorded the observation.
    public int LoggedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CultivationCycle CultivationCycle { get; set; } = null!;
    public User LoggedByUser { get; set; } = null!;
}
