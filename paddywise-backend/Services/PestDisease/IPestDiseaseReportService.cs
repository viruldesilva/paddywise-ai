using PaddyWise.Api.DTOs.PestDisease;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Services.PestDisease;

public interface IPestDiseaseReportService
{
    /// <summary>The officer review queue, or — for a farmer — only the diagnoses on their own
    /// observations. ?status=, ?observationId= and ?cultivationCycleId= narrow the list.</summary>
    Task<List<PestDiseaseReportResponseDto>> GetReportsAsync(
        int callerId, UserRole callerRole, string? status, int? observationId, int? cultivationCycleId);

    Task<PestDiseaseReportResponseDto?> GetByIdAsync(int reportId, int callerId, UserRole callerRole);

    Task<PestDiseaseReportResponseDto?> ReviewAsync(
        int reportId, int officerId, ReviewPestDiseaseReportDto request);
}
