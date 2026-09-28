using System.ComponentModel.DataAnnotations;

namespace PaddyWise.Api.DTOs.PestDisease;

/// <summary>One entry of the admin-managed pest/disease reference data.</summary>
public class PestDiseaseKnowledgeResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Symptoms { get; set; } = string.Empty;
    public string? FavorableConditions { get; set; }
    public string? CropStages { get; set; }
    public string ManagementGuidance { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>An admin adding or replacing one knowledge-base entry. Name must be unique
/// (case-insensitive) — it is what CropAnalysisAgent's get_pest_knowledge tool looks up by.</summary>
public class SavePestDiseaseKnowledgeRequestDto
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(150, ErrorMessage = "Name cannot exceed 150 characters.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>"Pest" or "Disease".</summary>
    [Required(ErrorMessage = "Category is required.")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Symptoms are required.")]
    [MaxLength(1000, ErrorMessage = "Symptoms cannot exceed 1000 characters.")]
    public string Symptoms { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Favorable conditions cannot exceed 500 characters.")]
    public string? FavorableConditions { get; set; }

    [MaxLength(200, ErrorMessage = "Crop stages cannot exceed 200 characters.")]
    public string? CropStages { get; set; }

    [Required(ErrorMessage = "Management guidance is required.")]
    [MaxLength(1000, ErrorMessage = "Management guidance cannot exceed 1000 characters.")]
    public string ManagementGuidance { get; set; } = string.Empty;

    [Required(ErrorMessage = "Source is required.")]
    [MaxLength(200, ErrorMessage = "Source cannot exceed 200 characters.")]
    public string Source { get; set; } = string.Empty;
}
