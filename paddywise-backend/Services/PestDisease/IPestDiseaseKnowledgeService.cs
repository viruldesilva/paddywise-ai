using PaddyWise.Api.DTOs.PestDisease;

namespace PaddyWise.Api.Services.PestDisease;

/// <summary>Admin CRUD over the PestDiseaseKnowledge reference table that
/// CropAnalysisAgent's get_pest_knowledge tool looks candidates up against.</summary>
public interface IPestDiseaseKnowledgeService
{
    Task<List<PestDiseaseKnowledgeResponseDto>> GetAllAsync();

    Task<PestDiseaseKnowledgeResponseDto?> GetByIdAsync(int id);

    /// <summary>Throws InvalidOperationException when the name already exists (case-insensitive).</summary>
    Task<PestDiseaseKnowledgeResponseDto> CreateAsync(SavePestDiseaseKnowledgeRequestDto request);

    /// <summary>Null when no entry has this id. Throws InvalidOperationException when the new
    /// name collides with a different entry's name.</summary>
    Task<PestDiseaseKnowledgeResponseDto?> UpdateAsync(int id, SavePestDiseaseKnowledgeRequestDto request);

    /// <summary>False when no entry has this id.</summary>
    Task<bool> DeleteAsync(int id);
}
