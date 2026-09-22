using PaddyWise.Api.DTOs.PestDisease;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Services.PestDisease;

public interface IObservationService
{
    /// <summary>A farmer's own observations, or — for an officer/admin — every observation,
    /// optionally narrowed to one cultivation cycle or field.</summary>
    Task<List<ObservationResponseDto>> GetObservationsAsync(
        int callerId, UserRole callerRole, int? cultivationCycleId, int? fieldId);

    Task<ObservationResponseDto?> GetByIdAsync(int observationId, int callerId, UserRole callerRole);

    Task<ObservationResponseDto> CreateAsync(int farmerId, CreateObservationRequestDto request);

    Task<ObservationResponseDto?> UpdateAsync(int observationId, int farmerId, UpdateObservationRequestDto request);

    /// <summary>Runs the Crop Analysis (Pest &amp; Disease Diagnosis) agent against an
    /// observation that has none yet, and persists one PestDiseaseReport per candidate issue
    /// it returns.</summary>
    Task<ObservationResponseDto?> RequestAnalysisAsync(int observationId, int farmerId);
}
