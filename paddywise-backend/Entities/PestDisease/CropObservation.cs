using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Entities.PestDisease;

/// <summary>A farmer-submitted report of a pest, disease or unknown problem on one cultivation
/// cycle. Feeds the Crop Analysis (Pest &amp; Disease Diagnosis) agent when analysis is requested.</summary>
public class CropObservation
{
    public int Id { get; set; }

    public int CultivationCycleId { get; set; }

    // The farmer who filed the report.
    public int ReportedByUserId { get; set; }

    public ObservationType ObservationType { get; set; }

    // Snapshot of the cycle's growth stage at report time — the cycle may move on to a later
    // stage before an officer reviews this, so the stage relevant to diagnosis must be stored
    // here rather than read live off CultivationCycle.CurrentStage.
    public GrowthStage CropStage { get; set; }

    /// <summary>The farmer's own words. Free-form and untrusted — goes in the agent's user
    /// prompt inside a delimited block, never the system prompt.</summary>
    public string Symptoms { get; set; } = string.Empty;

    public ObservationSeverity Severity { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public CultivationCycle CultivationCycle { get; set; } = null!;
    public User ReportedByUser { get; set; } = null!;
    public ICollection<PestDiseaseReport> Reports { get; set; } = new List<PestDiseaseReport>();
}
