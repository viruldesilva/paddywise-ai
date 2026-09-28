namespace PaddyWise.Api.Entities.PestDisease;

/// <summary>Admin-managed reference data the diagnosis agent looks candidates up against —
/// sourced from Department of Agriculture publications, never invented by the LLM.
/// Named PestDiseaseKnowledge (the work plan calls the table "PestDisease") to avoid a class
/// named identically to its own namespace segment.</summary>
public class PestDiseaseKnowledge
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public PestDiseaseCategory Category { get; set; }

    public string Symptoms { get; set; } = string.Empty;

    public string? FavorableConditions { get; set; }

    public string? CropStages { get; set; }

    public string ManagementGuidance { get; set; } = string.Empty;

    /// <summary>Citation, e.g. "Sri Lanka Department of Agriculture".</summary>
    public string Source { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
